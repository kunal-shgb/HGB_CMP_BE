using ComplaintManagement.Domain.Common;

namespace ComplaintManagement.Domain.Entities;

/// <summary>One-time code sent to a customer's registered mobile to view their complaint. Only a salted hash is kept.</summary>
public class TrackingOtp : Entity
{
    public Guid ComplaintId { get; set; }
    public required string CodeHash { get; set; }
    public required string Salt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public int FailedAttempts { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
}
