using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Domain.Common;
using ComplaintManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ComplaintManagement.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, TimeProvider clock)
    : DbContext(options), IApplicationDbContext
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
    public DbSet<ComplaintNumberSequence> ComplaintNumberSequences => Set<ComplaintNumberSequence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        modelBuilder.UseSnakeCaseNames();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow();
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default) entry.Entity.CreatedAt = now;
                if (entry.Entity.UpdatedAt == default) entry.Entity.UpdatedAt = entry.Entity.CreatedAt;
            }
            else if (entry.State == EntityState.Modified && !entry.Property(e => e.UpdatedAt).IsModified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>One row per year; the counter behind HGB-YYYY-NNNNNNNN.</summary>
public sealed class ComplaintNumberSequence
{
    public int Year { get; set; }
    public long LastValue { get; set; }
}
