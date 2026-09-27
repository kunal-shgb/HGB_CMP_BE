using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Application.Common.Security;
using ComplaintManagement.Contracts.Responses;

namespace ComplaintManagement.Application.Employees;

public interface IEmployeeDirectoryService
{
    /// <summary>Active employees the caller may assign complaints to.</summary>
    Task<IReadOnlyList<EmployeeResponse>> SearchAssignableAsync(string? query, CancellationToken ct);
}

public sealed class EmployeeDirectoryService(IIamUserService iam, IIamOrganisationService org, ICurrentUser user) : IEmployeeDirectoryService
{
    public async Task<IReadOnlyList<EmployeeResponse>> SearchAssignableAsync(string? query, CancellationToken ct)
    {
        var q = query?.Trim();
        if (q is { Length: > 100 }) q = q[..100];
        var candidates = await iam.SearchUsersAsync(q, ct);
        var assignable = await ComplaintScope.AssignableAsync(org, user, candidates, ct);
        return assignable
            .OrderBy(u => u.FullName)
            .Take(50)
            .Select(u => new EmployeeResponse(u.EmployeeCode, u.FullName, u.Designation, u.OfficeType, u.OfficeCode, u.OfficeName, u.DepartmentName))
            .ToList();
    }
}
