using ComplaintManagement.Domain.Common;

namespace ComplaintManagement.Domain.Entities;

/// <summary>
/// The customer's feedback on a closed complaint. One per closure: <see cref="ForClosedAt"/> is the complaint's
/// closure time it answers, so a complaint that is reopened and closed again can be rated again.
/// </summary>
public class ComplaintFeedback : Entity
{
    public Guid ComplaintId { get; set; }
    public DateTimeOffset ForClosedAt { get; set; }
    public bool Resolved { get; set; }
    /// <summary>Satisfaction from 1 (very poor) to 5 (very good).</summary>
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }

    // "Not resolved" feedback is flagged for staff until they mark it reviewed (or the complaint is reopened).
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ReviewedByName { get; set; }
    public string? ReviewNote { get; set; }
}

/// <summary>
/// A one-time feedback link for one closure of a complaint. Only a SHA-256 hash of the token is stored.
/// </summary>
public class FeedbackInvitation : Entity
{
    public Guid ComplaintId { get; set; }
    public required string TokenHash { get; set; }
    public DateTimeOffset ForClosedAt { get; set; }
    /// <summary>CLOSURE_MESSAGE (sent with the closure SMS/email) or TRACKING (shown after OTP).</summary>
    public required string Source { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
}
