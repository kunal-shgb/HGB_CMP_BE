using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Domain.Entities;
using ComplaintManagement.Domain.Enums;

namespace ComplaintManagement.Application.Common.Security;

/// <summary>Maker-checker routing: who must approve a Maker's decision, and whether a user may decide.</summary>
public static class ApprovalRules
{
    /// <summary>
    /// A Branch Maker is checked by the Regional Office the complaint's branch belongs to; an RO Maker (or one
    /// with an unrecognised office) by any Head Office Checker; an HO Maker by Checkers of the designated HO
    /// department (<paramref name="hoMakerCheckerDepartment"/>), or any HO Checker while none is set.
    /// </summary>
    public static (ScopeLevel Level, string? OfficeCode, string? Department) ApproverFor(
        ICurrentUser maker, Complaint complaint, string? hoMakerCheckerDepartment) =>
        maker.ScopeLevel switch
        {
            ScopeLevel.Branch => (ScopeLevel.Region, complaint.RegionCode, null),
            ScopeLevel.HeadOffice => (ScopeLevel.HeadOffice, null, string.IsNullOrWhiteSpace(hoMakerCheckerDepartment) ? null : hoMakerCheckerDepartment.Trim()),
            _ => (ScopeLevel.HeadOffice, null, null),
        };

    /// <summary>A Checker at the required level and office, who is not the Maker who asked (four eyes).</summary>
    public static bool CanDecide(ICurrentUser user, ComplaintApproval approval) =>
        approval.Status == ApprovalStatus.Pending
        && user.HasPermission(Permissions.ComplaintApprove)
        && !string.Equals(user.EmployeeId, approval.RequestedByEmployeeId, StringComparison.OrdinalIgnoreCase)
        && (approval.ApproverLevel == ScopeLevel.HeadOffice
            ? user.ScopeLevel == ScopeLevel.HeadOffice
              && (approval.ApproverDepartment is null
                  || string.Equals(user.DepartmentName, approval.ApproverDepartment, StringComparison.OrdinalIgnoreCase))
            : user.ScopeLevel == ScopeLevel.Region
              && string.Equals(user.OfficeCode, approval.ApproverOfficeCode, StringComparison.OrdinalIgnoreCase));

    /// <summary>The same rule as a query filter, for the Checker's queue.</summary>
    public static IQueryable<ComplaintApproval> DecidableBy(this IQueryable<ComplaintApproval> query, ICurrentUser user)
    {
        if (!user.HasPermission(Permissions.ComplaintApprove)) return query.Where(_ => false);
        var employeeId = user.EmployeeId;
        var office = user.OfficeCode;
        var department = user.DepartmentName?.ToUpper();
        var pending = query.Where(a => a.Status == ApprovalStatus.Pending && a.RequestedByEmployeeId != employeeId);
        return user.ScopeLevel switch
        {
            ScopeLevel.HeadOffice => pending.Where(a => a.ApproverLevel == ScopeLevel.HeadOffice
                && (a.ApproverDepartment == null || a.ApproverDepartment.ToUpper() == department)),
            ScopeLevel.Region when office is not null =>
                pending.Where(a => a.ApproverLevel == ScopeLevel.Region && a.ApproverOfficeCode == office),
            _ => pending.Where(_ => false),
        };
    }
}
