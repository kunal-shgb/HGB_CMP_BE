using System.Security.Cryptography;
using System.Text;
using ComplaintManagement.Application.Common;
using ComplaintManagement.Application.Feedback;
using ComplaintManagement.Contracts.Responses;
using ComplaintManagement.Contracts.Requests;
using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Domain.Common;
using ComplaintManagement.Domain.Entities;
using ComplaintManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ComplaintManagement.Application.Complaints;

public sealed class TrackingOptions
{
    public const string SectionName = "Tracking";

    public int OtpLifetimeMinutes { get; set; } = 10;
    public int MaxOtpAttempts { get; set; } = 5;
    /// <summary>Most codes one complaint can be sent in <see cref="OtpWindowMinutes"/>.</summary>
    public int MaxOtpsPerWindow { get; set; } = 3;
    public int OtpWindowMinutes { get; set; } = 15;
    /// <summary>
    /// Development only: return the code in the API response because no SMS gateway exists yet.
    /// The API refuses to start with this on in Production.
    /// </summary>
    public bool ExposeOtpForTesting { get; set; }
}

/// <summary>Customer self-service tracking: complaint number + registered mobile, verified by a one-time code.</summary>
public interface ITrackingService
{
    Task<TrackingOtpResponse> RequestOtpAsync(TrackingOtpRequest request, string? ipAddress, CancellationToken ct);
    Task<TrackingView> VerifyAsync(TrackingVerifyRequest request, string? ipAddress, CancellationToken ct);
}

public sealed class TrackingService(IApplicationDbContext db, TimeProvider clock, IOptions<TrackingOptions> options) : ITrackingService
{
    private const string Generic = "If the complaint number and mobile number match our records, a one-time code has been sent to that mobile.";
    private const string InvalidCode = "The code is incorrect or has expired. Please request a new one.";

    public async Task<TrackingOtpResponse> RequestOtpAsync(TrackingOtpRequest request, string? ipAddress, CancellationToken ct)
    {
        var o = options.Value;
        var complaint = await FindAsync(request.ComplaintNumber, request.Mobile, ct);
        var now = clock.GetUtcNow();
        Audit("TRACKING_OTP_REQUEST", complaint?.Id, ipAddress, complaint is null ? "no match" : null, now);
        if (complaint is null)
        {
            await db.SaveChangesAsync(ct);
            return new TrackingOtpResponse(Generic, o.OtpLifetimeMinutes);
        }

        var recent = await db.TrackingOtps.CountAsync(x => x.ComplaintId == complaint.Id && x.CreatedAt > now.AddMinutes(-o.OtpWindowMinutes), ct);
        if (recent >= o.MaxOtpsPerWindow)
            throw new DomainException("tracking.too_many_codes", $"Too many codes requested. Please try again after {o.OtpWindowMinutes} minutes.");

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var salt = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        db.TrackingOtps.Add(new TrackingOtp
        {
            ComplaintId = complaint.Id, CodeHash = Hash(code, salt), Salt = salt,
            CreatedAt = now, ExpiresAt = now.AddMinutes(o.OtpLifetimeMinutes),
        });
        db.Notifications.Add(new Notification
        {
            ComplaintId = complaint.Id, Event = NotificationEvents.TrackingOtp, Channel = NotificationChannel.Sms,
            Recipient = complaint.MobileNumber,
            Body = $"{code} is your one-time code to view complaint {complaint.ComplaintNumber}. It is valid for {o.OtpLifetimeMinutes} minutes. Do not share it. - Haryana Gramin Bank",
            CreatedAt = now, NextAttemptAt = now,
        });
        await db.SaveChangesAsync(ct);
        return new TrackingOtpResponse(Generic, o.OtpLifetimeMinutes, o.ExposeOtpForTesting ? code : null);
    }

    public async Task<TrackingView> VerifyAsync(TrackingVerifyRequest request, string? ipAddress, CancellationToken ct)
    {
        var o = options.Value;
        var now = clock.GetUtcNow();
        var complaint = await FindAsync(request.ComplaintNumber, request.Mobile, ct);
        var otp = complaint is null ? null : await db.TrackingOtps
            .Where(x => x.ComplaintId == complaint.Id && x.UsedAt == null && x.ExpiresAt > now)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        var code = (request.Otp ?? "").Trim();
        var valid = otp is not null && otp.FailedAttempts < o.MaxOtpAttempts && code.Length == 6
            && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Hash(code, otp.Salt)), Encoding.ASCII.GetBytes(otp.CodeHash));

        if (!valid)
        {
            if (otp is not null) otp.FailedAttempts++;
            Audit("TRACKING_VERIFY_FAILED", complaint?.Id, ipAddress, null, now);
            await db.SaveChangesAsync(ct);
            throw new DomainException("tracking.invalid_code", InvalidCode);
        }

        otp!.UsedAt = now;
        Audit("TRACKING_VIEW", complaint!.Id, ipAddress, null, now);
        await db.SaveChangesAsync(ct);
        return await BuildViewAsync(complaint.Id, ct);
    }

    private async Task<TrackingView> BuildViewAsync(Guid id, CancellationToken ct)
    {
        var c = await db.Complaints.AsNoTracking()
            .Include(x => x.Category).Include(x => x.Status)
            .Include(x => x.StatusHistory).Include(x => x.Remarks)
            .SingleAsync(x => x.Id == id, ct);
        var labels = await db.Statuses.AsNoTracking().ToDictionaryAsync(s => s.Code, s => s.CustomerLabel, ct);

        var updates = new List<TrackingUpdate>();
        string? lastLabel = null;
        foreach (var h in c.StatusHistory.OrderBy(h => h.ChangedAt))
        {
            // Show only changes the customer can see (label changes), never who made them.
            var label = labels.GetValueOrDefault(h.NewStatusCode, "Under process");
            if (label != lastLabel) updates.Add(new TrackingUpdate(h.ChangedAt, h.OldStatusCode is null ? "Complaint registered" : label, null));
            lastLabel = label;
        }
        updates.AddRange(c.Remarks
            .Where(r => r.Visibility == RemarkVisibility.Customer)
            .Select(r => new TrackingUpdate(r.CreatedAt, "Update from the Bank", r.Remark)));

        // A closed complaint still open for feedback gets a fresh one-time token for the feedback form.
        string? feedbackToken = null;
        var feedbackGiven = c.ClosedAt is { } closed
            && await db.ComplaintFeedback.AnyAsync(f => f.ComplaintId == c.Id && f.ForClosedAt == closed, ct);
        if (c.ClosedAt is not null && FeedbackRules.IsOpen(c, c.ClosedAt.Value, feedbackGiven, await FeedbackRules.WindowDaysAsync(db, ct), clock.GetUtcNow()))
        {
            feedbackToken = FeedbackRules.Invite(db, c, "TRACKING", clock.GetUtcNow());
            await db.SaveChangesAsync(ct);
        }

        return new TrackingView(
            c.ComplaintNumber, c.Status!.CustomerLabel, c.Title, c.Category!.Name, c.BranchName,
            c.CreatedAt, c.UpdatedAt, c.ResolvedAt, c.ClosedAt,
            updates.OrderByDescending(u => u.At).ToList(), feedbackToken, feedbackGiven);
    }

    private async Task<Complaint?> FindAsync(string? number, string? mobile, CancellationToken ct)
    {
        var n = (number ?? "").Trim().ToUpperInvariant();
        var m = new string((mobile ?? "").Where(char.IsDigit).ToArray());
        if (m.Length > 10) m = m[^10..]; // tolerate +91 / 0 prefixes
        if (n.Length is 0 or > 32 || m.Length != 10) return null;
        return await db.Complaints.FirstOrDefaultAsync(c => c.ComplaintNumber == n && c.MobileNumber == m, ct);
    }

    private void Audit(string action, Guid? complaintId, string? ip, string? details, DateTimeOffset now) =>
        db.AuditLogs.Add(new AuditLog
        {
            EmployeeId = PublicComplaintService.CustomerActor, Action = action, Module = "Tracking",
            RecordId = complaintId?.ToString(), IpAddress = ip, Details = details, CreatedAt = now,
        });

    private static string Hash(string code, string salt) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{salt}:{code}")));
}
