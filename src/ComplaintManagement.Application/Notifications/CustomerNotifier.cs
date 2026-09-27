using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Domain.Entities;

namespace ComplaintManagement.Application.Notifications;

/// <summary>
/// Decides which customer messages a change produces and stages them on the current unit of work.
/// Wording is provisional: SMS text must match the Bank's DLT-registered templates once a gateway is chosen.
/// </summary>
public sealed class CustomerNotifier(IApplicationDbContext db, TimeProvider clock)
{
    private const string Signature = "- Haryana Gramin Bank";

    public void Registered(Complaint c) => Enqueue(c, "REGISTERED",
        $"Complaint {c.ComplaintNumber} registered",
        $"Dear Customer, your complaint {c.ComplaintNumber} has been registered. We will keep you informed of its progress. {Signature}");

    /// <summary>
    /// Called when a complaint moves between statuses. Customers hear only about changes they can see:
    /// resolution, closure, or a change in the customer-facing status label.
    /// </summary>
    public void StatusChanged(Complaint c, ComplaintStatus from, ComplaintStatus to)
    {
        if (to.IsApprovalPending) return; // internal step; the customer still sees "Under process"
        if (to.IsResolution)
            Enqueue(c, "RESOLVED", $"Complaint {c.ComplaintNumber} resolved",
                $"Dear Customer, your complaint {c.ComplaintNumber} has been resolved. If you are not satisfied, please contact your branch. {Signature}");
        else if (to.IsTerminal)
            Enqueue(c, "CLOSED", $"Complaint {c.ComplaintNumber} closed",
                $"Dear Customer, your complaint {c.ComplaintNumber} has been closed ({to.CustomerLabel}). Thank you for banking with us. {Signature}");
        else if (!string.Equals(from.CustomerLabel, to.CustomerLabel, StringComparison.Ordinal))
            Enqueue(c, "STATUS_UPDATE", $"Complaint {c.ComplaintNumber}: {to.CustomerLabel}",
                $"Dear Customer, the status of your complaint {c.ComplaintNumber} is now: {to.CustomerLabel}. {Signature}");
    }

    private void Enqueue(Complaint c, string evt, string subject, string body)
    {
        var useEmail = c.PreferredChannel == NotificationChannel.Email && !string.IsNullOrWhiteSpace(c.Email);
        var now = clock.GetUtcNow();
        db.Notifications.Add(new Notification
        {
            ComplaintId = c.Id,
            Event = evt,
            Channel = useEmail ? NotificationChannel.Email : NotificationChannel.Sms,
            Recipient = useEmail ? c.Email! : c.MobileNumber,
            Subject = useEmail ? subject : null,
            Body = body,
            CreatedAt = now,
            NextAttemptAt = now,
        });
    }
}
