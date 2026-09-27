namespace ComplaintManagement.Application.Common.Interfaces;

/// <summary>Employee directory lookups against the Bank IAM. Implemented in Infrastructure/IAM.</summary>
public interface IIamUserService
{
    Task<IamUser?> GetUserAsync(string employeeCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IamUser>> SearchUsersAsync(string? query, CancellationToken cancellationToken = default);
}

/// <summary>What the portal needs from an IAM user profile. Contact details are deliberately left out.</summary>
public sealed record IamUser(
    string EmployeeCode,
    string FullName,
    string? Designation,
    string? AccessRole,
    string? OfficeType,
    string? OfficeCode,
    string? OfficeName,
    string? DepartmentName,
    bool IsActive);
