using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Domain.Entities;
using ComplaintManagement.Domain.Enums;

namespace ComplaintManagement.Application.Common.Security;

/// <summary>
/// Who may assign a complaint. A branch's complaints are assigned by its OfficeHead; a Regional Office Checker
/// may assign them only when the branch has no active OfficeHead. Other assigning roles are unchanged.
/// </summary>
public sealed class AssignmentPolicy(IIamUserService iam, IRoleMappingService mappings)
{
    public async Task<bool> CanAssignAsync(ICurrentUser user, Complaint complaint, CancellationToken ct)
    {
        if (!user.HasPermission(Permissions.ComplaintAssign)) return false;
        if (!AssignsOnlyAsRegionalChecker(user)) return true;
        return !await BranchHasOfficeHeadAsync(complaint.BranchCode, ct);
    }

    public async Task DemandAsync(ICurrentUser user, Complaint complaint, CancellationToken ct)
    {
        if (await CanAssignAsync(user, complaint, ct)) return;
        throw new Exceptions.ForbiddenAccessException(user.HasPermission(Permissions.ComplaintAssign)
            ? "This branch has an office head, who assigns its complaints."
            : "Only the office head can assign or reassign this complaint.");
    }

    /// <summary>True when the user's right to assign comes only from being a Checker at a Regional Office.</summary>
    private static bool AssignsOnlyAsRegionalChecker(ICurrentUser user) =>
        user.ScopeLevel == ScopeLevel.Region
        && user.Roles.Contains(AppRoles.Checker)
        && !Permissions.ForRoles(user.Roles.Where(r => r != AppRoles.Checker)).Contains(Permissions.ComplaintAssign);

    /// <summary>Whether the IAM lists an active employee at the branch whose access role maps to OfficeHead there.</summary>
    public async Task<bool> BranchHasOfficeHeadAsync(string branchCode, CancellationToken ct)
    {
        var active = await mappings.GetActiveMappingsAsync(ct);
        var staff = await iam.GetUsersInOfficeAsync(branchCode, ct);
        return staff.Any(u => u.IsActive
            && RoleMappingService.Resolve(active, u.AccessRole is null ? [] : [u.AccessRole], u.OfficeType).Contains(AppRoles.OfficeHead));
    }
}
