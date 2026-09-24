using ComplaintManagement.Domain.Enums;

namespace ComplaintManagement.Application.Common.Interfaces;

/// <summary>
/// The authenticated employee, built from Bank IAM claims. This is the only source of
/// employee identity for business logic and audit; never take it from a request body.
/// </summary>
public interface ICurrentUser
{
    string EmployeeId { get; }
    string Name { get; }
    string? Designation { get; }
    IReadOnlySet<string> Roles { get; }
    ScopeLevel ScopeLevel { get; }
    string? RegionCode { get; }
    string? BranchCode { get; }
    string? DepartmentCode { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }

    bool HasPermission(string permission);
}
