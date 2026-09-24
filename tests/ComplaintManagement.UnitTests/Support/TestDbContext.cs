using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ComplaintManagement.UnitTests.Support;

internal sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options), IApplicationDbContext
{
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<ComplaintStatusHistory> ComplaintStatusHistory => Set<ComplaintStatusHistory>();
    public DbSet<ComplaintAssignment> ComplaintAssignments => Set<ComplaintAssignment>();
    public DbSet<ComplaintRemark> ComplaintRemarks => Set<ComplaintRemark>();
    public DbSet<ComplaintAttachment> ComplaintAttachments => Set<ComplaintAttachment>();
    public DbSet<ComplaintCategory> Categories => Set<ComplaintCategory>();
    public DbSet<ComplaintSubCategory> SubCategories => Set<ComplaintSubCategory>();
    public DbSet<ComplaintStatus> Statuses => Set<ComplaintStatus>();
    public DbSet<ComplaintStatusTransition> StatusTransitions => Set<ComplaintStatusTransition>();
    public DbSet<ComplaintPriority> Priorities => Set<ComplaintPriority>();
    public DbSet<Region> Regions => Set<Region>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<ApplicationRoleMapping> ApplicationRoleMappings => Set<ApplicationRoleMapping>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<ComplaintStatus>().HasKey(s => s.Code);
        b.Entity<ComplaintPriority>().HasKey(p => p.Code);
        b.Entity<Complaint>().HasOne(c => c.Status).WithMany().HasForeignKey(c => c.StatusCode);
        b.Entity<Complaint>().HasOne(c => c.Priority).WithMany().HasForeignKey(c => c.PriorityCode);
        b.Entity<Complaint>().HasMany(c => c.StatusHistory).WithOne().HasForeignKey(h => h.ComplaintId);
        b.Entity<Complaint>().HasMany(c => c.Assignments).WithOne().HasForeignKey(h => h.ComplaintId);
        b.Entity<Complaint>().HasMany(c => c.Remarks).WithOne().HasForeignKey(h => h.ComplaintId);
        b.Entity<Complaint>().HasMany(c => c.Attachments).WithOne().HasForeignKey(h => h.ComplaintId);
    }

    public static TestDbContext Create() =>
        new(new DbContextOptionsBuilder<TestDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
