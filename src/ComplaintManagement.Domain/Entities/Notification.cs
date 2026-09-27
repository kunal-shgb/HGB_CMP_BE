using ComplaintManagement.Domain.Common;

namespace ComplaintManagement.Domain.Entities;

/// <summary>
/// A customer message waiting to be sent or already sent (transactional outbox). Rows are written in the
/// same save as the change that caused them, then delivered by the dispatch job through the SMS/email gateway.
/// </summary>
public class Notification : Entity
{
    public Guid? ComplaintId { get; set; }
    /// <summary>REGISTERED, STATUS_UPDATE, RESOLVED, CLOSED or TRACKING_OTP.</summary>
    public required string Event { get; set; }
    /// <summary>SMS or EMAIL.</summary>
    public required string Channel { get; set; }
    /// <summary>Mobile number or email address; needed to deliver, never logged in full.</summary>
    public required string Recipient { get; set; }
    public string? Subject { get; set; }
    public required string Body { get; set; }
    /// <summary>PENDING, SENT or FAILED.</summary>
    public string Status { get; set; } = NotificationStatus.Pending;
    public int Attempts { get; set; }
    public DateTimeOffset NextAttemptAt { get; set; }
    public string? LastError { get; set; }
    public string? ProviderReference { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
}

public static class NotificationEvents
{
    /// <summary>One-time code for complaint tracking. Its body is blanked once sent and never logged.</summary>
    public const string TrackingOtp = "TRACKING_OTP";
    public const string RedactedBody = "[one-time code, removed after sending]";
}

public static class NotificationStatus
{
    public const string Pending = "PENDING";
    public const string Sent = "SENT";
    public const string Failed = "FAILED";
}

public static class NotificationChannel
{
    public const string Sms = "SMS";
    public const string Email = "EMAIL";
}
