using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComplaintManagement.Application.Notifications;

public sealed record SendResult(bool Success, string? ProviderReference = null, string? Error = null);

/// <summary>Delivers one message through the Bank's SMS or email gateway. Implemented in Infrastructure.</summary>
public interface INotificationSender
{
    Task<SendResult> SendAsync(Notification message, CancellationToken ct);
}

public interface INotificationDispatcher
{
    /// <summary>Sends due messages. Returns how many were attempted.</summary>
    Task<int> DispatchDueAsync(CancellationToken ct);
}

/// <summary>Sends due outbox messages with retries: 1, 2, 4, 8 minutes apart, then FAILED after five attempts.</summary>
public sealed class NotificationDispatcher(IApplicationDbContext db, INotificationSender sender, TimeProvider clock, ILogger<NotificationDispatcher> logger)
    : INotificationDispatcher
{
    public const int MaxAttempts = 5;
    private const int BatchSize = 50;

    public async Task<int> DispatchDueAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var due = await db.Notifications
            .Where(n => n.Status == NotificationStatus.Pending && n.NextAttemptAt <= now)
            .OrderBy(n => n.NextAttemptAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        foreach (var n in due)
        {
            SendResult result;
            try
            {
                result = await sender.SendAsync(n, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                result = new SendResult(false, Error: ex.GetType().Name);
            }

            n.Attempts++;
            if (result.Success)
            {
                n.Status = NotificationStatus.Sent;
                n.SentAt = clock.GetUtcNow();
                n.ProviderReference = result.ProviderReference;
                n.LastError = null;
                if (n.Event == NotificationEvents.TrackingOtp) n.Body = NotificationEvents.RedactedBody;
            }
            else
            {
                n.LastError = result.Error is { Length: > 500 } e ? e[..500] : result.Error;
                if (n.Attempts >= MaxAttempts)
                {
                    n.Status = NotificationStatus.Failed;
                    logger.LogWarning("Notification {Id} failed after {Attempts} attempts", n.Id, n.Attempts);
                }
                else
                {
                    n.NextAttemptAt = clock.GetUtcNow().AddMinutes(Math.Pow(2, n.Attempts - 1));
                }
            }
        }
        if (due.Count > 0) await db.SaveChangesAsync(ct);
        return due.Count;
    }
}
