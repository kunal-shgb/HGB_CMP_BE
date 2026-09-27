using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComplaintManagement.Application.Escalation;

/// <summary>Automatic escalation of overdue complaints, run by the background job.</summary>
public interface IEscalationService
{
    /// <summary>Escalates every open complaint past its thresholds. Returns how many were escalated.</summary>
    Task<int> RunAsync(CancellationToken ct);
}

public sealed class EscalationService(IApplicationDbContext db, TimeProvider clock, ILogger<EscalationService> logger) : IEscalationService
{
    public const string SystemActor = "SYSTEM";
    private const int BatchSize = 200;

    public async Task<int> RunAsync(CancellationToken ct)
    {
        var settings = await EscalationSettings.LoadAsync(db, ct);
        if (!settings.Enabled) return 0;

        var now = clock.GetUtcNow();
        var total = 0;
        // Highest level first, so a long-overdue complaint jumps straight to Head Office.
        foreach (var (level, days) in new[]
                 {
                     (EscalationLevels.HeadOffice, settings.ToHeadOfficeAfterDays),
                     (EscalationLevels.RegionalOffice, settings.ToRegionalOfficeAfterDays),
                 })
        {
            var dueBefore = now.AddDays(-days);
            while (!ct.IsCancellationRequested)
            {
                var batch = await db.Complaints
                    // Resolved complaints are only waiting to be closed, so they are not escalated.
                    .Where(c => c.ClosedAt == null && c.ResolvedAt == null && c.SlaDueDate != null
                        && c.SlaDueDate <= dueBefore && c.EscalationLevel < level)
                    .OrderBy(c => c.SlaDueDate)
                    .Take(BatchSize)
                    .ToListAsync(ct);
                if (batch.Count == 0) break;

                foreach (var c in batch)
                {
                    var overdueDays = Math.Max(0, (int)Math.Floor((now - c.SlaDueDate!.Value).TotalDays));
                    var reason = $"TAT exceeded by {overdueDays} day{(overdueDays == 1 ? "" : "s")}";
                    db.ComplaintEscalations.Add(new ComplaintEscalation
                    {
                        ComplaintId = c.Id, FromLevel = c.EscalationLevel, ToLevel = level, Reason = reason,
                        EscalatedBy = SystemActor, EscalatedByName = "Automatic escalation", EscalatedAt = now,
                    });
                    db.AuditLogs.Add(new AuditLog
                    {
                        EmployeeId = SystemActor, Action = "ESCALATE", Module = "Complaint", RecordId = c.Id.ToString(),
                        Details = $"level {c.EscalationLevel}->{level}: {reason}", CreatedAt = now,
                    });
                    c.EscalationLevel = level;
                    c.UpdatedAt = now;
                }
                await db.SaveChangesAsync(ct);
                total += batch.Count;
            }
        }

        if (total > 0) logger.LogInformation("Escalated {Count} overdue complaints", total);
        return total;
    }
}
