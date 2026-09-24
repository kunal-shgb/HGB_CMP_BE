namespace ComplaintManagement.Application.Common.Interfaces;

/// <summary>Employee directory lookups against the Bank IAM. Implemented in Infrastructure/IAM.</summary>
public interface IIamUserService
{
    Task<IamUser?> GetUserAsync(string employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IamUser>> SearchUsersAsync(string? query, CancellationToken cancellationToken = default);
}

public sealed record IamUser(
    string EmployeeId,
    string Name,
    string? Email,
    string? Designation,
    IReadOnlyList<string> IamRoles,
    string? RegionCode,
    string? BranchCode,
    string? DepartmentCode);
