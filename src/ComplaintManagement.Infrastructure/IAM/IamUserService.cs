using ComplaintManagement.Application.Common.Interfaces;

namespace ComplaintManagement.Infrastructure.IAM;

/// <summary>
/// Bank IAM directory client. The Bank's user-management API contract has not been shared yet,
/// so this is a seam: implement it once the IAM specification (protocol, endpoints, claim names) is fixed.
/// </summary>
internal sealed class IamUserService : IIamUserService
{
    public Task<IamUser?> GetUserAsync(string employeeCode, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Bank IAM directory integration is pending the IAM API specification.");

    public Task<IReadOnlyList<IamUser>> SearchUsersAsync(string? query, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Bank IAM directory integration is pending the IAM API specification.");

    public Task<IReadOnlyList<IamUser>> GetUsersInOfficeAsync(string officeCode, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Bank IAM directory integration is pending the IAM API specification.");
}
