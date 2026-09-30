using ComplaintManagement.Application.Common.Exceptions;
using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Contracts.Requests;
using ComplaintManagement.Contracts.Responses;
using ComplaintManagement.Domain.Common;
using ComplaintManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ComplaintManagement.Application.Feedback;

/// <summary>Customer feedback through a one-time link. Anonymous; the token is the only credential.</summary>
public interface IFeedbackService
{
    Task<PublicFeedbackView> GetAsync(string token, CancellationToken ct);
    Task<SubmitFeedbackResponse> SubmitAsync(string token, SubmitFeedbackRequest request, string? ipAddress, CancellationToken ct);
}

public sealed class FeedbackService(IApplicationDbContext db, TimeProvider clock) : IFeedbackService
{
    private const string Customer = "CUSTOMER";

    public async Task<PublicFeedbackView> GetAsync(string token, CancellationToken ct)
    {
        var (invitation, complaint) = await FindAsync(token, ct);
        return new PublicFeedbackView(complaint.ComplaintNumber, complaint.Title, complaint.ClosedAt, await StateAsync(invitation, complaint, ct));
    }

    public async Task<SubmitFeedbackResponse> SubmitAsync(string token, SubmitFeedbackRequest r, string? ipAddress, CancellationToken ct)
    {
        if (r.Resolved is null) throw Invalid("resolved", "Tell us whether your issue was resolved.");
        if (r.Rating is < 1 or > 5) throw Invalid("rating", "Choose a rating from 1 to 5.");
        var comment = string.IsNullOrWhiteSpace(r.Comment) ? null : r.Comment.Trim();
        if (comment?.Length > FeedbackRules.MaxComment) throw Invalid("comment", $"Keep your comment under {FeedbackRules.MaxComment} characters.");

        var (invitation, complaint) = await FindAsync(token, ct);
        var state = await StateAsync(invitation, complaint, ct);
        if (state != "OPEN")
            throw new DomainException("feedback.closed", state == "SUBMITTED"
                ? "Feedback has already been given for this complaint. Thank you."
                : "This feedback link has expired.");

        var now = clock.GetUtcNow();
        db.ComplaintFeedback.Add(new ComplaintFeedback
        {
            ComplaintId = complaint.Id, ForClosedAt = complaint.ClosedAt!.Value,
            Resolved = r.Resolved.Value, Rating = r.Rating, Comment = comment, SubmittedAt = now,
        });
        invitation.UsedAt = now;
        db.AuditLogs.Add(new AuditLog
        {
            EmployeeId = Customer, Action = "FEEDBACK", Module = "Complaint", RecordId = complaint.Id.ToString(),
            IpAddress = ipAddress, Details = $"resolved={r.Resolved.Value} rating={r.Rating} via={invitation.Source}", CreatedAt = now,
        });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Unique index on (complaint, closure): the same closure was rated through another link meanwhile.
            throw new DomainException("feedback.closed", "Feedback has already been given for this complaint. Thank you.");
        }

        return r.Resolved.Value
            ? new SubmitFeedbackResponse("Thank you for your feedback.", false)
            : new SubmitFeedbackResponse(
                "Thank you. We are sorry your issue is not resolved. The Bank will review your complaint and may contact you.", true);
    }

    private async Task<(FeedbackInvitation Invitation, Complaint Complaint)> FindAsync(string token, CancellationToken ct)
    {
        var t = (token ?? "").Trim();
        // Tokens are 43 URL-safe characters; anything else cannot match and is not looked up.
        if (t.Length is < 20 or > 64 || !t.All(ch => char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_'))
            throw new NotFoundException("Feedback link", "invalid");
        var hash = FeedbackRules.Hash(t);
        var invitation = await db.FeedbackInvitations.FirstOrDefaultAsync(i => i.TokenHash == hash, ct)
            ?? throw new NotFoundException("Feedback link", "invalid");
        var complaint = await db.Complaints.SingleAsync(c => c.Id == invitation.ComplaintId, ct);
        return (invitation, complaint);
    }

    private async Task<string> StateAsync(FeedbackInvitation invitation, Complaint complaint, CancellationToken ct)
    {
        var given = invitation.UsedAt is not null
            || await db.ComplaintFeedback.AnyAsync(f => f.ComplaintId == complaint.Id && f.ForClosedAt == invitation.ForClosedAt, ct);
        if (given) return "SUBMITTED";
        var open = FeedbackRules.IsOpen(complaint, invitation.ForClosedAt, false, await FeedbackRules.WindowDaysAsync(db, ct), clock.GetUtcNow());
        return open ? "OPEN" : "EXPIRED";
    }

    private static FluentValidation.ValidationException Invalid(string field, string message) =>
        new([new FluentValidation.Results.ValidationFailure(field, message)]);
}
