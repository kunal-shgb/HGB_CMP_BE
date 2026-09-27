using ComplaintManagement.Domain.Enums;

namespace ComplaintManagement.Application.Common.Interfaces;

/// <summary>
/// The authenticated employee, built from the Bank IAM user profile. This is the only source of
/// employee identity for business logic and audit; never take it from a request body.
/// </summary>
public interface ICurrentUser
{
    /// <summary>IAM employeeCode.</summary>
    string EmployeeId { get; }
    string Name { get; }
    string? Designation { get; }
    IReadOnlySet<string> Roles { get; }

    /// <summary>From the IAM officeType. Null when the office type is not recognised: the user then sees only complaints assigned to them.</summary>
    ScopeLevel? ScopeLevel { get; }
    string? OfficeType { get; }
    /// <summary>IAM officeCode: the branch code for branch users, the RO code for Regional Office users.</summary>
    string? OfficeCode { get; }
    string? OfficeName { get; }
    string? DepartmentName { get; }

    string? IpAddress { get; }
    string? UserAgent { get; }

    bool HasPermission(string permission);
}
