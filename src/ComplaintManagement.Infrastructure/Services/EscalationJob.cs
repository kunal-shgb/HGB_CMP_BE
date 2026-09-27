using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Application.Escalation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ComplaintManagement.Infrastructure.Services;

public sealed class EscalationJobOptions
{
    public const string SectionName = "Escalation";

    /// <summary>Run the automatic escalation job on this instance. The rules themselves are Admin settings.</summary>
    public bool JobEnabled { get; set; } = true;

    public int IntervalMinutes { get; set; } = 15;
}

/// <summary>Periodically escalates overdue complaints. Safe on several instances: each run takes a distributed lock.</summary>
internal sealed class EscalationJob(IServiceScopeFactory scopes, IDistributedLock locks, IOptions<EscalationJobOptions> options, ILogger<EscalationJob> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.JobEnabled)
        {
            logger.LogInformation("Automatic escalation job is disabled on this instance");
            return;
        }

        var interval = TimeSpan.FromMinutes(Math.Max(1, options.Value.IntervalMinutes));
        // Small delay so start-up (migrations, seeding) finishes first.
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        using var timer = new PeriodicTimer(interval);
        do
        {
            try
            {
                await using var handle = await locks.TryAcquireAsync("escalation-job", interval, stoppingToken);
                if (handle is null) continue; // another instance is running it

                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IEscalationService>().RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Keep the job alive; the next tick retries.
                logger.LogError(ex, "Automatic escalation run failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
