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
    public DbSet<ApplicationRoleMapping> ApplicationRoleMappings => Set<ApplicationRoleMapping>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ComplaintApproval> ComplaintApprovals => Set<ComplaintApproval>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<ComplaintEscalation> ComplaintEscalations => Set<ComplaintEscalation>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<TrackingOtp> TrackingOtps => Set<TrackingOtp>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<ComplaintStatus>().HasKey(s => s.Code);
        b.Entity<ComplaintPriority>().HasKey(p => p.Code);
        b.Entity<AppSetting>().HasKey(x => x.Key);
        b.Entity<ComplaintStatusTransition>().Property(t => t.Id).ValueGeneratedNever();
        b.Entity<Complaint>().HasOne(c => c.Status).WithMany().HasForeignKey(c => c.StatusCode);
        b.Entity<Complaint>().HasOne(c => c.Priority).WithMany().HasForeignKey(c => c.PriorityCode);
        b.Entity<Complaint>().HasMany(c => c.StatusHistory).WithOne().HasForeignKey(h => h.ComplaintId);
        b.Entity<Complaint>().HasMany(c => c.Assignments).WithOne().HasForeignKey(h => h.ComplaintId);
        b.Entity<Complaint>().HasMany(c => c.Remarks).WithOne().HasForeignKey(h => h.ComplaintId);
        b.Entity<Complaint>().HasMany(c => c.Attachments).WithOne().HasForeignKey(h => h.ComplaintId);
        b.Entity<Complaint>().HasMany(c => c.Escalations).WithOne().HasForeignKey(h => h.ComplaintId);
        b.Entity<ComplaintApproval>().HasOne(a => a.Complaint).WithMany().HasForeignKey(a => a.ComplaintId);
        b.Entity<ComplaintApproval>().Property(a => a.Version).IsConcurrencyToken();
    }

    public static TestDbContext Create() =>
        new(new DbContextOptionsBuilder<TestDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
