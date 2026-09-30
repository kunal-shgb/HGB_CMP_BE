using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ComplaintManagement.Infrastructure.IAM.Mock;

/// <summary>Settings for the temporary mock IAM. Refused in Production.</summary>
public sealed class MockIamOptions
{
    public const string SectionName = "MockIam";

    public bool Enabled { get; set; }

    /// <summary>
    /// Initial password given to seeded dummy users. A secret: dotnet user-secrets ("MockIam:SeedPassword")
    /// or an environment variable, never appsettings.
    /// </summary>
    public string? SeedPassword { get; set; }
}

public static class MockIamSetup
{
    /// <summary>Creates the mock_iam schema if missing and adds any seed organisation data and users not there yet.</summary>
    public static async Task EnsureAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MockIamDbContext>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<MockIamOptions>>().Value;
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(MockIamSetup));

        // Rebuild when any table or column the model expects is missing: the store is disposable scaffolding.
        var present = (await db.Database
            .SqlQuery<string>($"SELECT table_name || '.' || column_name AS \"Value\" FROM information_schema.columns WHERE table_schema = 'mock_iam'")
            .ToListAsync(ct)).ToHashSet();
        var expected = db.Model.GetEntityTypes().SelectMany(e =>
        {
            var table = Microsoft.EntityFrameworkCore.Metadata.StoreObjectIdentifier.Table(e.GetTableName()!, e.GetSchema());
            return e.GetProperties().Select(p => $"{e.GetTableName()}.{p.GetColumnName(table)}");
        });
        if (!expected.All(present.Contains))
        {
            // The mock store is disposable test scaffolding: rebuild it whenever its shape changes.
            logger.LogWarning("Creating temporary mock IAM store (schema {Schema})", MockIamDbContext.Schema);
            await db.Database.ExecuteSqlRawAsync("DROP SCHEMA IF EXISTS mock_iam CASCADE", ct);
            await db.Database.ExecuteSqlRawAsync(db.Database.GenerateCreateScript(), ct);
        }

        var regions = await db.Regions.Select(r => r.Code).ToListAsync(ct);
        db.Regions.AddRange(MockIamSeedData.Regions.Where(r => !regions.Contains(r.Code))
            .Select(r => new MockIamRegion { Code = r.Code, Name = r.Name }));
        var branches = await db.Branches.Select(b => b.Code).ToListAsync(ct);
        db.Branches.AddRange(MockIamSeedData.Branches.Where(b => !branches.Contains(b.Code))
            .Select(b => new MockIamBranch { Code = b.Code, Name = b.Name, RegionCode = b.RegionCode }));
        var departments = await db.Departments.Select(d => d.Code).ToListAsync(ct);
        db.Departments.AddRange(MockIamSeedData.Departments.Where(d => !departments.Contains(d.Code))
            .Select(d => new MockIamDepartment { Code = d.Code, Name = d.Name }));
        await db.SaveChangesAsync(ct);

        // Keep existing dummy users' departments in step with the seed list (e.g. RO divisions added later).
        var seeded = MockIamSeedData.Users.ToDictionary(u => u.EmployeeCode);
        foreach (var user in await db.Users.ToListAsync(ct))
        {
            if (!seeded.TryGetValue(user.EmployeeCode, out var seed) || user.DepartmentName == seed.DepartmentName) continue;
            user.DepartmentId = seed.DepartmentId;
            user.DepartmentName = seed.DepartmentName;
            user.UpdatedAt = DateTimeOffset.UtcNow;
        }
        await db.SaveChangesAsync(ct);

        var existing = await db.Users.Select(u => u.EmployeeCode).ToListAsync(ct);
        var missing = MockIamSeedData.Users.Where(u => !existing.Contains(u.EmployeeCode)).ToList();
        if (missing.Count == 0) return;

        var now = DateTimeOffset.UtcNow;
        foreach (var u in missing)
        {
            db.Users.Add(new MockIamUser
            {
                EmployeeCode = u.EmployeeCode, FullName = u.FullName, Designation = u.Designation, AccessRole = u.AccessRole,
                DepartmentId = u.DepartmentId, DepartmentName = u.DepartmentName, OfficeId = u.OfficeId, OfficeCode = u.OfficeCode,
                OfficeName = u.OfficeName, OfficeType = u.OfficeType, Mobile = u.Mobile, IsActive = u.IsActive, IsSystemAdmin = u.IsSystemAdmin,
                PasswordHash = MockPasswordHasher.Hash(options.SeedPassword!), CreatedAt = now, UpdatedAt = now,
            });
        }
        await db.SaveChangesAsync(ct);
        logger.LogWarning("Seeded {Count} dummy users into the mock IAM store", missing.Count);
    }
}
