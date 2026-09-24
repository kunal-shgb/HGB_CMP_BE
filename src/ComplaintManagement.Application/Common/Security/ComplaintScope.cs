using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Domain.Entities;
using ComplaintManagement.Domain.Enums;

namespace ComplaintManagement.Application.Common.Security;

/// <summary>Restricts complaint queries to the caller's HO / RO / Branch / Department scope.</summary>
public static class ComplaintScope
{
    public static IQueryable<Complaint> VisibleTo(this IQueryable<Complaint> query, ICurrentUser user)
    {
        // Complaints assigned to the caller personally are always visible to them.
        var employeeId = user.EmployeeId;
        return user.ScopeLevel switch
        {
            ScopeLevel.HeadOffice => query,
            ScopeLevel.Department when user.DepartmentCode is { } department =>
                query.Where(c => c.AssignedEmployeeId == employeeId
                    || (c.AssignedDepartment != null && c.AssignedDepartment.Code == department)),
            ScopeLevel.Region when user.RegionCode is { } region =>
                query.Where(c => c.AssignedEmployeeId == employeeId || c.Branch!.Region!.Code == region),
            ScopeLevel.Branch when user.BranchCode is { } branch =>
                query.Where(c => c.AssignedEmployeeId == employeeId || c.Branch!.Code == branch),
            // A scoped role without the matching org claim sees only what is assigned to them.
            _ => query.Where(c => c.AssignedEmployeeId == employeeId),
        };
    }

    /// <summary>Whether an employee falls inside the caller's scope, for assignment targets.</summary>
    public static bool CanTarget(ICurrentUser user, IamUser target) => user.ScopeLevel switch
    {
        ScopeLevel.HeadOffice => true,
        ScopeLevel.Department => target.DepartmentCode is not null && target.DepartmentCode == user.DepartmentCode,
        ScopeLevel.Region => target.RegionCode is not null && target.RegionCode == user.RegionCode,
        ScopeLevel.Branch => target.BranchCode is not null && target.BranchCode == user.BranchCode,
        _ => false,
    };
}
