using ComplaintManagement.Application.Common;
using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Application.Notifications;
using ComplaintManagement.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ComplaintManagement.Infrastructure.Notifications;

public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    /// <summary>
    /// "Log" writes messages to the application log instead of sending them (not allowed in Production).
    /// "None" leaves messages queued. Real SMS/email gateways are added here once the Bank chooses them.
    /// </summary>
    public string Provider { get; set; } = "None";

    public int IntervalSeconds { get; set; } = 30;
}

/// <summary>
/// Stand-in for the SMS/email gateways: logs the message with a masked recipient and reports success.
/// </summary>
internal sealed class LogNotificationSender(ILogger<LogNotificationSender> logger) : INotificationSender
{
    public Task<SendResult> SendAsync(Notification message, CancellationToken ct)
    {
        var to = message.Channel == NotificationChannel.Email ? Masking.Email(message.Recipient) : Masking.Mobile(message.Recipient);
        var body = message.Event == NotificationEvents.TrackingOtp ? NotificationEvents.RedactedBody : message.Body;
        logger.LogInformation("[{Channel} to {Recipient}] {Event}: {Body}", message.Channel, to, message.Event, body);
        return Task.FromResult(new SendResult(true, $"log-{Guid.NewGuid():N}"));
    }
}

/// <summary>Sends queued customer messages every few seconds. Takes a distributed lock so only one instance sends.</summary>
internal sealed class NotificationDispatchJob(IServiceScopeFactory scopes, IDistributedLock locks, IOptions<NotificationOptions> options, ILogger<NotificationDispatchJob> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(5, options.Value.IntervalSeconds));
        using var timer = new PeriodicTimer(interval);
        do
        {
            try
            {
                await using var handle = await locks.TryAcquireAsync("notification-dispatch", TimeSpan.FromMinutes(5), stoppingToken);
                if (handle is null) continue;
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<INotificationDispatcher>().DispatchDueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Notification dispatch run failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
