using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Domain.Entities;
using ComplaintManagement.Infrastructure.Persistence;

namespace ComplaintManagement.Infrastructure.Services;

internal sealed class AuditLogger(ApplicationDbContext db, ICurrentUser user, TimeProvider clock) : IAuditLogger
{
    public void Log(string action, string module, string? recordId, string? details = null)
    {
        db.AuditLogs.Add(new AuditLog
        {
            EmployeeId = user.EmployeeId,
            Action = action,
            Module = module,
            RecordId = recordId,
            IpAddress = user.IpAddress,
            UserAgent = Truncate(user.UserAgent, 512),
            Details = Truncate(details, 1000),
            CreatedAt = clock.GetUtcNow(),
        });
    }

    private static string? Truncate(string? value, int max) => value is null || value.Length <= max ? value : value[..max];
}
