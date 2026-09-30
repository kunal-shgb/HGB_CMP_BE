using ComplaintManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ComplaintManagement.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Complaint> Complaints { get; }
    DbSet<ComplaintFeedback> ComplaintFeedback { get; }
    DbSet<FeedbackInvitation> FeedbackInvitations { get; }
    DbSet<ComplaintCategoryGroup> CategoryGroups { get; }
    DbSet<ComplaintStatusHistory> ComplaintStatusHistory { get; }
    DbSet<ComplaintAssignment> ComplaintAssignments { get; }
    DbSet<ComplaintRemark> ComplaintRemarks { get; }
    DbSet<ComplaintAttachment> ComplaintAttachments { get; }
    DbSet<ComplaintCategory> Categories { get; }
    DbSet<ComplaintStatus> Statuses { get; }
    DbSet<ComplaintStatusTransition> StatusTransitions { get; }
    DbSet<ComplaintPriority> Priorities { get; }
    DbSet<ApplicationRoleMapping> ApplicationRoleMappings { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<ComplaintApproval> ComplaintApprovals { get; }
    DbSet<AppSetting> AppSettings { get; }
    DbSet<ComplaintEscalation> ComplaintEscalations { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<TrackingOtp> TrackingOtps { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
