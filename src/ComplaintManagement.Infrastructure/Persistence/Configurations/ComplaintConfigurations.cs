using ComplaintManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplaintManagement.Infrastructure.Persistence.Configurations;

internal sealed class ComplaintConfiguration : IEntityTypeConfiguration<Complaint>
{
    public void Configure(EntityTypeBuilder<Complaint> b)
    {
        b.ToTable("complaints");
        b.HasKey(x => x.Id);
        b.Property(x => x.ComplaintNumber).HasMaxLength(32);
        b.HasIndex(x => x.ComplaintNumber).IsUnique();
        b.Property(x => x.CustomerName).HasMaxLength(150);
        b.Property(x => x.MobileNumber).HasMaxLength(15);
        b.Property(x => x.Email).HasMaxLength(254);
        b.Property(x => x.CustomerId).HasMaxLength(32);
        b.Property(x => x.AccountNumber).HasMaxLength(34);
        b.Property(x => x.PreferredChannel).HasMaxLength(16);
        b.Property(x => x.Title).HasMaxLength(150);
        b.Property(x => x.Description).HasMaxLength(4000);
        b.Property(x => x.TransactionId).HasMaxLength(64);
        b.Property(x => x.TransactionAmount).HasPrecision(18, 2);
        b.Property(x => x.PriorityCode).HasMaxLength(40);
        b.Property(x => x.StatusCode).HasMaxLength(40);
        b.Property(x => x.AssignedEmployeeId).HasMaxLength(32);
        b.Property(x => x.AssignedEmployeeName).HasMaxLength(150);
        b.Property(x => x.BranchCode).HasMaxLength(32);
        b.Property(x => x.BranchName).HasMaxLength(150);
        b.Property(x => x.RegionCode).HasMaxLength(32);
        b.Property(x => x.RegionName).HasMaxLength(150);
        b.Property(x => x.AssignedDepartmentCode).HasMaxLength(50);
        b.Property(x => x.AssignedDepartmentName).HasMaxLength(150);
        b.Property(x => x.Source).HasMaxLength(32).HasDefaultValue(ComplaintSources.Website);
        b.Property(x => x.LodgedByEmployeeId).HasMaxLength(32);
        b.Property(x => x.LodgedByName).HasMaxLength(150);
        b.Property(x => x.LodgedByOfficeName).HasMaxLength(150);
        b.Property(x => x.EscalatedDivisionCode).HasMaxLength(50);
        b.Property(x => x.EscalatedDivisionName).HasMaxLength(150);

        b.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Status).WithMany().HasForeignKey(x => x.StatusCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Priority).WithMany().HasForeignKey(x => x.PriorityCode).OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.StatusHistory).WithOne().HasForeignKey(x => x.ComplaintId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Assignments).WithOne().HasForeignKey(x => x.ComplaintId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Remarks).WithOne().HasForeignKey(x => x.ComplaintId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Attachments).WithOne().HasForeignKey(x => x.ComplaintId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Escalations).WithOne().HasForeignKey(x => x.ComplaintId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Feedback).WithOne().HasForeignKey(x => x.ComplaintId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.CreatedAt);
        b.HasIndex(x => x.StatusCode);
        b.HasIndex(x => x.Source);
        b.HasIndex(x => x.MobileNumber);
        b.HasIndex(x => x.AccountNumber);
        b.HasIndex(x => x.TransactionId);
        b.HasIndex(x => x.AssignedEmployeeId);
        b.HasIndex(x => x.BranchCode);
        b.HasIndex(x => x.RegionCode);
        b.HasIndex(x => x.AssignedDepartmentCode);
        b.HasIndex(x => new { x.ClosedAt, x.SlaDueDate });
        b.HasIndex(x => x.EscalationLevel);
    }
}

internal sealed class ComplaintStatusHistoryConfiguration : IEntityTypeConfiguration<ComplaintStatusHistory>
{
    public void Configure(EntityTypeBuilder<ComplaintStatusHistory> b)
    {
        b.ToTable("complaint_status_history");
        b.Property(x => x.OldStatusCode).HasMaxLength(40);
        b.Property(x => x.NewStatusCode).HasMaxLength(40);
        b.Property(x => x.Remarks).HasMaxLength(4000);
        b.Property(x => x.ChangedByEmployeeId).HasMaxLength(32);
        b.Property(x => x.ChangedByName).HasMaxLength(150);
        b.HasIndex(x => new { x.ComplaintId, x.ChangedAt });
    }
}

internal sealed class ComplaintAssignmentConfiguration : IEntityTypeConfiguration<ComplaintAssignment>
{
    public void Configure(EntityTypeBuilder<ComplaintAssignment> b)
    {
        b.ToTable("complaint_assignments");
        b.Property(x => x.AssignedFromEmployeeId).HasMaxLength(32);
        b.Property(x => x.AssignedToEmployeeId).HasMaxLength(32);
        b.Property(x => x.AssignedToName).HasMaxLength(150);
        b.Property(x => x.AssignedByEmployeeId).HasMaxLength(32);
        b.Property(x => x.Remarks).HasMaxLength(4000);
        b.Property(x => x.AssignedDepartmentCode).HasMaxLength(50);
        b.HasIndex(x => new { x.ComplaintId, x.AssignedAt });
    }
}

internal sealed class ComplaintRemarkConfiguration : IEntityTypeConfiguration<ComplaintRemark>
{
    public void Configure(EntityTypeBuilder<ComplaintRemark> b)
    {
        b.ToTable("complaint_remarks");
        b.Property(x => x.Remark).HasMaxLength(4000);
        b.Property(x => x.Visibility).HasConversion<string>().HasMaxLength(16);
        b.Property(x => x.CreatedByEmployeeId).HasMaxLength(32);
        b.Property(x => x.CreatedByName).HasMaxLength(150);
        b.HasIndex(x => new { x.ComplaintId, x.CreatedAt });
    }
}

internal sealed class ComplaintAttachmentConfiguration : IEntityTypeConfiguration<ComplaintAttachment>
{
    public void Configure(EntityTypeBuilder<ComplaintAttachment> b)
    {
        b.ToTable("complaint_attachments");
        b.Property(x => x.FileName).HasMaxLength(255);
        b.Property(x => x.StorageKey).HasMaxLength(512);
        b.Property(x => x.ContentType).HasMaxLength(128);
        b.Property(x => x.UploadedBy).HasMaxLength(32);
        b.Property(x => x.UploadedByName).HasMaxLength(150);
        b.Property(x => x.Sha256).HasMaxLength(64);
        b.Property(x => x.ScanStatus).HasMaxLength(16);
        b.HasIndex(x => x.ComplaintId);
        b.HasOne<ComplaintRemark>().WithMany(r => r.Attachments).HasForeignKey(x => x.RemarkId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_logs");
        b.Property(x => x.EmployeeId).HasMaxLength(32);
        b.Property(x => x.Action).HasMaxLength(64);
        b.Property(x => x.Module).HasMaxLength(64);
        b.Property(x => x.RecordId).HasMaxLength(64);
        b.Property(x => x.IpAddress).HasMaxLength(64);
        b.Property(x => x.UserAgent).HasMaxLength(512);
        b.Property(x => x.Details).HasMaxLength(1000);
        b.HasIndex(x => x.CreatedAt);
        b.HasIndex(x => new { x.Module, x.RecordId });
        b.HasIndex(x => x.EmployeeId);
    }
}

internal sealed class ComplaintApprovalConfiguration : IEntityTypeConfiguration<ComplaintApproval>
{
    public void Configure(EntityTypeBuilder<ComplaintApproval> b)
    {
        b.ToTable("complaint_approvals");
        b.HasOne(x => x.Complaint).WithMany().HasForeignKey(x => x.ComplaintId).OnDelete(DeleteBehavior.Cascade);
        b.Property(x => x.RequestedStatusCode).HasMaxLength(40);
        b.Property(x => x.PreviousStatusCode).HasMaxLength(40);
        b.Property(x => x.MakerRemarks).HasMaxLength(4000);
        b.Property(x => x.RequestedByEmployeeId).HasMaxLength(32);
        b.Property(x => x.RequestedByName).HasMaxLength(150);
        b.Property(x => x.RequestedByOfficeName).HasMaxLength(150);
        b.Property(x => x.ApproverLevel).HasConversion<string>().HasMaxLength(16);
        b.Property(x => x.ApproverOfficeCode).HasMaxLength(32);
        b.Property(x => x.ApproverDepartment).HasMaxLength(50);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        b.Property(x => x.DecidedByEmployeeId).HasMaxLength(32);
        b.Property(x => x.DecidedByName).HasMaxLength(150);
        b.Property(x => x.DecisionRemarks).HasMaxLength(4000);
        // Maps to PostgreSQL's xmin system column: optimistic concurrency between Checkers.
        b.Property(x => x.Version).HasColumnName("xmin").HasColumnType("xid").IsRowVersion();
        b.HasOne<ComplaintStatus>().WithMany().HasForeignKey(x => x.RequestedStatusCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ComplaintStatus>().WithMany().HasForeignKey(x => x.PreviousStatusCode).OnDelete(DeleteBehavior.Restrict);

        // At most one open request per complaint.
        b.HasIndex(x => x.ComplaintId).IsUnique().HasFilter("status = 'Pending'").HasDatabaseName("ux_complaint_approvals_one_pending");
        b.HasIndex(x => new { x.Status, x.ApproverLevel, x.ApproverOfficeCode });
    }
}

internal sealed class ComplaintEscalationConfiguration : IEntityTypeConfiguration<ComplaintEscalation>
{
    public void Configure(EntityTypeBuilder<ComplaintEscalation> b)
    {
        b.ToTable("complaint_escalations");
        b.Property(x => x.Reason).HasMaxLength(4000);
        b.Property(x => x.EscalatedBy).HasMaxLength(32);
        b.Property(x => x.EscalatedByName).HasMaxLength(150);
        b.Property(x => x.ToDivisionCode).HasMaxLength(50);
        b.Property(x => x.ToDivisionName).HasMaxLength(150);
        b.HasIndex(x => new { x.ComplaintId, x.EscalatedAt });
    }
}

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.ToTable("notification_outbox");
        b.Property(x => x.Event).HasMaxLength(32);
        b.Property(x => x.Channel).HasMaxLength(8);
        b.Property(x => x.Recipient).HasMaxLength(254);
        b.Property(x => x.Subject).HasMaxLength(200);
        b.Property(x => x.Body).HasMaxLength(2000);
        b.Property(x => x.Status).HasMaxLength(16);
        b.Property(x => x.LastError).HasMaxLength(500);
        b.Property(x => x.ProviderReference).HasMaxLength(100);
        b.HasOne<Complaint>().WithMany().HasForeignKey(x => x.ComplaintId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.Status, x.NextAttemptAt });
        b.HasIndex(x => x.ComplaintId);
    }
}

internal sealed class TrackingOtpConfiguration : IEntityTypeConfiguration<TrackingOtp>
{
    public void Configure(EntityTypeBuilder<TrackingOtp> b)
    {
        b.ToTable("tracking_otps");
        b.Property(x => x.CodeHash).HasMaxLength(64);
        b.Property(x => x.Salt).HasMaxLength(32);
        b.HasOne<Complaint>().WithMany().HasForeignKey(x => x.ComplaintId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.ComplaintId, x.CreatedAt });
    }
}

internal sealed class ComplaintNumberSequenceConfiguration : IEntityTypeConfiguration<ComplaintNumberSequence>
{
    public void Configure(EntityTypeBuilder<ComplaintNumberSequence> b)
    {
        b.ToTable("complaint_number_sequences");
        b.HasKey(x => x.Year);
        b.Property(x => x.Year).ValueGeneratedNever();
    }
}

internal sealed class ComplaintFeedbackConfiguration : IEntityTypeConfiguration<ComplaintFeedback>
{
    public void Configure(EntityTypeBuilder<ComplaintFeedback> b)
    {
        b.ToTable("complaint_feedback");
        b.Property(x => x.Comment).HasMaxLength(1000);
        b.Property(x => x.ReviewedBy).HasMaxLength(32);
        b.Property(x => x.ReviewedByName).HasMaxLength(150);
        b.Property(x => x.ReviewNote).HasMaxLength(1000);
        // One feedback per closure of a complaint.
        b.HasIndex(x => new { x.ComplaintId, x.ForClosedAt }).IsUnique();
        b.HasIndex(x => x.SubmittedAt);
    }
}

internal sealed class FeedbackInvitationConfiguration : IEntityTypeConfiguration<FeedbackInvitation>
{
    public void Configure(EntityTypeBuilder<FeedbackInvitation> b)
    {
        b.ToTable("feedback_invitations");
        b.Property(x => x.TokenHash).HasMaxLength(64);
        b.Property(x => x.Source).HasMaxLength(32);
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasOne<Complaint>().WithMany().HasForeignKey(x => x.ComplaintId).OnDelete(DeleteBehavior.Cascade);
    }
}
