using ComplaintManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplaintManagement.Infrastructure.IAM.Mock;

/// <summary>
/// Separate context for the temporary mock IAM store (users and organisation data) so the product schema
/// and its migrations never contain a credentials table or organisation master data. The schema is created on startup when MockIam is enabled.
/// </summary>
public sealed class MockIamDbContext(DbContextOptions<MockIamDbContext> options) : DbContext(options)
{
    public const string Schema = "mock_iam";

    public DbSet<MockIamUser> Users => Set<MockIamUser>();
    public DbSet<MockIamRegion> Regions => Set<MockIamRegion>();
    public DbSet<MockIamBranch> Branches => Set<MockIamBranch>();
    public DbSet<MockIamDepartment> Departments => Set<MockIamDepartment>();

    /// <summary>Tables the mock store must have; if any is missing the schema is rebuilt.</summary>
    public static readonly string[] Tables = ["users", "regions", "branches", "departments"];

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        var b = modelBuilder.Entity<MockIamUser>();
        b.ToTable("users");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.EmployeeCode).IsUnique();
        b.Property(x => x.EmployeeCode).HasMaxLength(32);
        b.Property(x => x.FullName).HasMaxLength(150);
        b.Property(x => x.Designation).HasMaxLength(100);
        b.Property(x => x.AccessRole).HasMaxLength(50);
        b.Property(x => x.DepartmentName).HasMaxLength(50);
        b.Property(x => x.OfficeCode).HasMaxLength(32);
        b.Property(x => x.OfficeName).HasMaxLength(150);
        b.Property(x => x.OfficeType).HasMaxLength(50);
        b.Property(x => x.Mobile).HasMaxLength(15);
        b.Property(x => x.PasswordHash).HasMaxLength(200);

        var r = modelBuilder.Entity<MockIamRegion>();
        r.ToTable("regions");
        r.HasKey(x => x.Code);
        r.Property(x => x.Code).HasMaxLength(32);
        r.Property(x => x.Name).HasMaxLength(150);

        var br = modelBuilder.Entity<MockIamBranch>();
        br.ToTable("branches");
        br.HasKey(x => x.Code);
        br.Property(x => x.Code).HasMaxLength(32);
        br.Property(x => x.Name).HasMaxLength(150);
        br.Property(x => x.RegionCode).HasMaxLength(32);
        br.HasOne<MockIamRegion>().WithMany().HasForeignKey(x => x.RegionCode);

        var d = modelBuilder.Entity<MockIamDepartment>();
        d.ToTable("departments");
        d.HasKey(x => x.Code);
        d.Property(x => x.Code).HasMaxLength(50);
        d.Property(x => x.Name).HasMaxLength(150);

        modelBuilder.UseSnakeCaseNames();
    }
}
