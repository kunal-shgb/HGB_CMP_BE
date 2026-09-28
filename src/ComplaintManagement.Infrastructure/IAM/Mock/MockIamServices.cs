using ComplaintManagement.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ComplaintManagement.Infrastructure.IAM.Mock;

/// <summary>Verifies credentials against the mock IAM users table. Stand-in for <see cref="IamAuthenticator"/>.</summary>
internal sealed class MockIamAuthenticator(MockIamDbContext db) : IIamAuthenticator
{
    public async Task<IamUser?> AuthenticateAsync(string employeeCode, string password, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.EmployeeCode == employeeCode, cancellationToken);
        // Always run the hash check, even for unknown codes, to keep timing uniform.
        var ok = MockPasswordHasher.Verify(password, user?.PasswordHash);
        return ok && user is not null ? ToIamUser(user) : null;
    }

    internal static IamUser ToIamUser(MockIamUser u) =>
        new(u.EmployeeCode, u.FullName, u.Designation, u.AccessRole, u.OfficeType, u.OfficeCode, u.OfficeName, u.DepartmentName, u.IsActive, u.IsSystemAdmin);
}

/// <summary>Employee directory backed by the mock IAM users table.</summary>
internal sealed class MockIamUserService(MockIamDbContext db) : IIamUserService
{
    public async Task<IamUser?> GetUserAsync(string employeeCode, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.EmployeeCode == employeeCode, cancellationToken);
        return user is null ? null : MockIamAuthenticator.ToIamUser(user);
    }

    public async Task<IReadOnlyList<IamUser>> GetUsersInOfficeAsync(string officeCode, CancellationToken cancellationToken = default)
    {
        var rows = await db.Users.AsNoTracking().Where(u => u.OfficeCode == officeCode).ToListAsync(cancellationToken);
        return rows.Select(MockIamAuthenticator.ToIamUser).ToList();
    }

    public async Task<IReadOnlyList<IamUser>> SearchUsersAsync(string? query, CancellationToken cancellationToken = default)
    {
        var users = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim().ToLower();
            users = users.Where(u => u.FullName.ToLower().Contains(q) || u.EmployeeCode.Contains(q));
        }
        var rows = await users.OrderBy(u => u.FullName).Take(200).ToListAsync(cancellationToken);
        return rows.Select(MockIamAuthenticator.ToIamUser).ToList();
    }
}
