using ComplaintManagement.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace ComplaintManagement.Infrastructure.IAM;

/// <summary>Employee directory backed by the DevAuth identities. Development only.</summary>
internal sealed class DevDirectoryIamUserService(IOptions<DevIdentityOptions> options) : IIamUserService
{
    public Task<IamUser?> GetUserAsync(string employeeId, CancellationToken cancellationToken = default) =>
        Task.FromResult(options.Value.Users
            .Where(u => string.Equals(u.EmployeeId, employeeId, StringComparison.OrdinalIgnoreCase))
            .Select(ToIamUser)
            .FirstOrDefault());

    public Task<IReadOnlyList<IamUser>> SearchUsersAsync(string? query, CancellationToken cancellationToken = default)
    {
        IEnumerable<DevIdentity> users = options.Value.Users;
        if (!string.IsNullOrWhiteSpace(query))
        {
            users = users.Where(u =>
                u.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                u.EmployeeId.Contains(query, StringComparison.OrdinalIgnoreCase));
        }
        return Task.FromResult<IReadOnlyList<IamUser>>(users.Select(ToIamUser).ToList());
    }

    internal static IamUser ToIamUser(DevIdentity u) =>
        new(u.EmployeeId, u.Name, u.Email, u.Designation, u.IamRoles, u.RegionCode, u.BranchCode, u.DepartmentCode);
}
