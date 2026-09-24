using ComplaintManagement.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ComplaintManagement.Application.Common.Security;

public interface IRoleMappingService
{
    /// <summary>Active IAM role → application role pairs.</summary>
    Task<IReadOnlyList<(string IamRole, string ApplicationRole)>> GetActiveMappingsAsync(CancellationToken ct);
}

public sealed class RoleMappingService(IApplicationDbContext db) : IRoleMappingService
{
    public async Task<IReadOnlyList<(string IamRole, string ApplicationRole)>> GetActiveMappingsAsync(CancellationToken ct)
    {
        var rows = await db.ApplicationRoleMappings.AsNoTracking()
            .Where(m => m.IsActive)
            .Select(m => new { m.IamRole, m.ApplicationRole })
            .ToListAsync(ct);
        return rows.Select(r => (r.IamRole, r.ApplicationRole)).ToList();
    }
}
