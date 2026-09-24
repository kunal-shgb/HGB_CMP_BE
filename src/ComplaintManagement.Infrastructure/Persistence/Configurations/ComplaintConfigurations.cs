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
        b.Property(x => x.Description).HasMaxLength(4000);
        b.Property(x => x.TransactionId).HasMaxLength(64);
        b.Property(x => x.TransactionAmount).HasPrecision(18, 2);
        b.Property(x => x.PriorityCode).HasMaxLength(40);
        b.Property(x => x.StatusCode).HasMaxLength(40);
        b.Property(x => x.AssignedEmployeeId).HasMaxLength(32);
        b.Property(x => x.AssignedEmployeeName).HasMaxLength(150);

        b.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.SubCategory).WithMany().HasForeignKey(x => x.SubCategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Status).WithMany().HasForeignKey(x => x.StatusCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Priority).WithMany().HasForeignKey(x => x.PriorityCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AssignedDepartment).WithMany().HasForeignKey(x => x.AssignedDepartmentId).OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.StatusHistory).WithOne().HasForeignKey(x => x.ComplaintId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Assignments).WithOne().HasForeignKey(x => x.ComplaintId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Remarks).WithOne().HasForeignKey(x => x.ComplaintId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Attachments).WithOne().HasForeignKey(x => x.ComplaintId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.CreatedAt);
        b.HasIndex(x => x.StatusCode);
        b.HasIndex(x => x.MobileNumber);
        b.HasIndex(x => x.AccountNumber);
        b.HasIndex(x => x.TransactionId);
        b.HasIndex(x => x.AssignedEmployeeId);
        b.HasIndex(x => new { x.ClosedAt, x.SlaDueDate });
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
        b.HasOne<Department>().WithMany().HasForeignKey(x => x.AssignedDepartmentId).OnDelete(DeleteBehavior.Restrict);
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

internal sealed class ComplaintNumberSequenceConfiguration : IEntityTypeConfiguration<ComplaintNumberSequence>
{
    public void Configure(EntityTypeBuilder<ComplaintNumberSequence> b)
    {
        b.ToTable("complaint_number_sequences");
        b.HasKey(x => x.Year);
        b.Property(x => x.Year).ValueGeneratedNever();
    }
}
