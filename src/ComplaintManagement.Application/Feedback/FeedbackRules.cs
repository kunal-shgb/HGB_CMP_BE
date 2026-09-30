using System.Security.Cryptography;
using System.Text;
using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ComplaintManagement.Application.Feedback;

public sealed class FeedbackOptions
{
    public const string SectionName = "Feedback";

    /// <summary>
    /// Public page that takes a feedback token, e.g. "https://www.hgb.example/feedback". The token is appended
    /// as a path segment. While empty, closure messages carry no link (customers can still use tracking).
    /// </summary>
    public string? LinkBaseUrl { get; set; }
}

/// <summary>Who may give feedback, when, and how feedback links are made.</summary>
public static class FeedbackRules
{
    public const int DefaultWindowDays = 30;
    public const int MaxComment = 1000;

    public static async Task<int> WindowDaysAsync(IApplicationDbContext db, CancellationToken ct)
    {
        var raw = await db.AppSettings.AsNoTracking().Where(s => s.Key == AppSettingKeys.FeedbackWindowDays).Select(s => s.Value).FirstOrDefaultAsync(ct);
        return int.TryParse(raw, out var days) && days is >= 1 and <= 365 ? days : DefaultWindowDays;
    }

    /// <summary>
    /// Open for feedback: the complaint is closed, this is about its current closure, the window has not
    /// passed, and no feedback has been given for this closure yet.
    /// </summary>
    public static bool IsOpen(Complaint c, DateTimeOffset forClosedAt, bool alreadyGiven, int windowDays, DateTimeOffset now) =>
        c.ClosedAt is { } closed && closed == forClosedAt && !alreadyGiven && now <= closed.AddDays(windowDays);

    /// <summary>A new random token (URL-safe) and the hash stored for it.</summary>
    public static (string Token, string Hash) NewToken()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return (token, Hash(token));
    }

    public static string Hash(string token) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    /// <summary>Stages a one-time feedback invitation for the complaint's current closure and returns its token.</summary>
    public static string Invite(IApplicationDbContext db, Complaint c, string source, DateTimeOffset now)
    {
        var (token, hash) = NewToken();
        db.FeedbackInvitations.Add(new FeedbackInvitation
        {
            ComplaintId = c.Id, TokenHash = hash, ForClosedAt = c.ClosedAt!.Value, Source = source, CreatedAt = now,
        });
        return token;
    }

    /// <summary>"Not resolved" feedback on the current closure that staff have not reviewed yet.</summary>
    public static bool NeedsReview(ComplaintFeedback f, Complaint c) =>
        !f.Resolved && f.ReviewedAt is null && c.ClosedAt == f.ForClosedAt;
}
