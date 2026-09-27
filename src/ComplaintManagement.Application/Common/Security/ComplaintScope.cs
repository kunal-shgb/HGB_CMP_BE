using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Domain.Entities;
using ComplaintManagement.Domain.Enums;

namespace ComplaintManagement.Application.Common.Security;

/// <summary>
/// Complaint visibility by office:
/// Branch → complaints logged for that branch; Regional Office → complaints of every branch under it;
/// Head Office → all complaints. A complaint assigned to the user personally is always visible to them.
/// </summary>
public static class ComplaintScope
{
    public static IQueryable<Complaint> VisibleTo(this IQueryable<Complaint> query, ICurrentUser user)
    {
        var employeeId = user.EmployeeId;
        var office = user.OfficeCode;
        return user.ScopeLevel switch
        {
            ScopeLevel.HeadOffice => query,
            ScopeLevel.Region when office is not null =>
                query.Where(c => c.AssignedEmployeeId == employeeId || c.RegionCode == office),
            ScopeLevel.Branch when office is not null =>
                query.Where(c => c.AssignedEmployeeId == employeeId || c.BranchCode == office),
            // Unrecognised office type or missing office code: only what is assigned to them.
            _ => query.Where(c => c.AssignedEmployeeId == employeeId),
        };
    }

    /// <summary>
    /// Whether the caller may assign work to this employee: HO to anyone; an RO to its own staff or
    /// staff of branches under it; a branch to its own staff. Inactive employees are never valid targets.
    /// </summary>
    public static async Task<bool> CanTargetAsync(IIamOrganisationService org, ICurrentUser user, IamUser target, CancellationToken ct)
    {
        if (!target.IsActive) return false;
        switch (user.ScopeLevel)
        {
            case ScopeLevel.HeadOffice:
                return true;
            case ScopeLevel.Branch:
                return user.OfficeCode is not null && SameOffice(user, target);
            case ScopeLevel.Region when user.OfficeCode is not null:
                if (SameOffice(user, target)) return true;
                return target.OfficeCode is not null
                    && await org.FindBranchAsync(target.OfficeCode, ct) is { } branch
                    && string.Equals(branch.RegionCode, user.OfficeCode, StringComparison.OrdinalIgnoreCase);
            default:
                return false;
        }
    }

    /// <summary>Filters a directory result down to valid assignment targets for the caller.</summary>
    public static async Task<IReadOnlyList<IamUser>> AssignableAsync(IIamOrganisationService org, ICurrentUser user, IEnumerable<IamUser> candidates, CancellationToken ct)
    {
        var active = candidates.Where(c => c.IsActive).ToList();
        switch (user.ScopeLevel)
        {
            case ScopeLevel.HeadOffice:
                return active;
            case ScopeLevel.Branch when user.OfficeCode is not null:
                return active.Where(c => SameOffice(user, c)).ToList();
            case ScopeLevel.Region when user.OfficeCode is not null:
                var branchCodes = (await org.GetBranchesAsync(ct))
                    .Where(b => string.Equals(b.RegionCode, user.OfficeCode, StringComparison.OrdinalIgnoreCase))
                    .Select(b => b.Code);
                var allowed = branchCodes.Append(user.OfficeCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
                return active.Where(c => c.OfficeCode is not null && allowed.Contains(c.OfficeCode)).ToList();
            default:
                return [];
        }
    }

    private static bool SameOffice(ICurrentUser user, IamUser target) =>
        string.Equals(target.OfficeCode, user.OfficeCode, StringComparison.OrdinalIgnoreCase);
}
