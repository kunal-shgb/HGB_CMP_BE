using ComplaintManagement.Application.Common.Exceptions;
using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Contracts.Responses;
using ComplaintManagement.Domain.Entities;

namespace ComplaintManagement.Application.Common.Security;

/// <summary>
/// What a user may do on one complaint. A role grants an action on every complaint in the user's scope;
/// the person a complaint is assigned to may also work that complaint (except reassigning it), even without
/// such a role. This is how a branch OfficeHead delegates a complaint to a colleague.
/// </summary>
public static class ComplaintAccess
{
    /// <summary>Actions the assignee of a complaint may take on it regardless of role.</summary>
    private static readonly HashSet<string> Delegated =
    [
        Permissions.ComplaintViewUnmasked,
        Permissions.ComplaintChangeStatus,
        Permissions.ComplaintAddRemark,
        Permissions.ComplaintAddAttachment,
        Permissions.ComplaintEscalate,
    ];

    public static bool IsAssignee(ICurrentUser user, Complaint complaint) =>
        complaint.AssignedEmployeeId is not null
        && string.Equals(complaint.AssignedEmployeeId, user.EmployeeId, StringComparison.OrdinalIgnoreCase);

    public static bool Can(ICurrentUser user, Complaint complaint, string permission) =>
        user.HasPermission(permission) || (Delegated.Contains(permission) && IsAssignee(user, complaint));

    public static void Demand(ICurrentUser user, Complaint complaint, string permission)
    {
        if (!Can(user, complaint, permission))
            throw new ForbiddenAccessException(permission == Permissions.ComplaintAssign
                ? "Only the office head can assign or reassign this complaint."
                : "You can view this complaint, but only the office head or the person it is assigned to can act on it.");
    }

    public static ComplaintAbilities Abilities(ICurrentUser user, Complaint complaint, bool canEscalate, bool canAssign) => new(
        ChangeStatus: Can(user, complaint, Permissions.ComplaintChangeStatus),
        AddRemark: Can(user, complaint, Permissions.ComplaintAddRemark),
        AddAttachment: Can(user, complaint, Permissions.ComplaintAddAttachment),
        Assign: canAssign,
        Escalate: canEscalate,
        ViewCustomerDetails: Can(user, complaint, Permissions.ComplaintViewUnmasked),
        IsAssignedToMe: IsAssignee(user, complaint));
}
