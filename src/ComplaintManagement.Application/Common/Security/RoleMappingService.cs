using ComplaintManagement.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ComplaintManagement.Application.Common.Security;

public sealed record RoleMapping(string IamRole, string? OfficeType, string ApplicationRole);

public interface IRoleMappingService
{
    Task<IReadOnlyList<RoleMapping>> GetActiveMappingsAsync(CancellationToken ct);
}

public sealed class RoleMappingService(IApplicationDbContext db) : IRoleMappingService
{
    public async Task<IReadOnlyList<RoleMapping>> GetActiveMappingsAsync(CancellationToken ct) =>
        await db.ApplicationRoleMappings.AsNoTracking()
            .Where(m => m.IsActive)
            .Select(m => new RoleMapping(m.IamRole, m.OfficeType, m.ApplicationRole))
            .ToListAsync(ct);

    /// <summary>
    /// Application roles for an IAM access role at an office type. Rows with no office type apply
    /// everywhere; rows with one apply only there.
    /// </summary>
    public static IReadOnlyList<string> Resolve(IEnumerable<RoleMapping> mappings, IEnumerable<string> iamRoles, string? officeType)
    {
        var roles = iamRoles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return mappings
            .Where(m => roles.Contains(m.IamRole)
                && (m.OfficeType is null || string.Equals(m.OfficeType, officeType, StringComparison.OrdinalIgnoreCase)))
            .Select(m => m.ApplicationRole)
            .Distinct()
            .ToList();
    }
}
