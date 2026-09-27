using ComplaintManagement.Domain.Entities;
using ComplaintManagement.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplaintManagement.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<ComplaintCategory>
{
    public void Configure(EntityTypeBuilder<ComplaintCategory> b)
    {
        b.ToTable("complaint_categories");
        b.Property(x => x.Code).HasMaxLength(40);
        b.Property(x => x.Name).HasMaxLength(150);
        b.Property(x => x.GroupName).HasMaxLength(100);
        b.Property(x => x.Description).HasMaxLength(500);
        b.HasIndex(x => x.Code).IsUnique();
        b.HasMany(x => x.SubCategories).WithOne(x => x.Category).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasData(ReferenceSeed.Categories);
    }
}

internal sealed class SubCategoryConfiguration : IEntityTypeConfiguration<ComplaintSubCategory>
{
    public void Configure(EntityTypeBuilder<ComplaintSubCategory> b)
    {
        b.ToTable("complaint_subcategories");
        b.Property(x => x.Code).HasMaxLength(40);
        b.Property(x => x.Name).HasMaxLength(150);
        b.Property(x => x.DefaultPriorityCode).HasMaxLength(40);
        b.HasIndex(x => new { x.CategoryId, x.Code }).IsUnique();
        b.Property(x => x.DefaultDepartmentCode).HasMaxLength(50);
        b.HasOne<ComplaintPriority>().WithMany().HasForeignKey(x => x.DefaultPriorityCode).OnDelete(DeleteBehavior.Restrict);
        b.HasData(ReferenceSeed.SubCategories);
    }
}

internal sealed class StatusConfiguration : IEntityTypeConfiguration<ComplaintStatus>
{
    public void Configure(EntityTypeBuilder<ComplaintStatus> b)
    {
        b.ToTable("complaint_statuses");
        b.HasKey(x => x.Code);
        b.Property(x => x.Code).HasMaxLength(40);
        b.Property(x => x.Name).HasMaxLength(100);
        b.Property(x => x.CustomerLabel).HasMaxLength(100);
        b.HasData(ReferenceSeed.Statuses);
    }
}

internal sealed class StatusTransitionConfiguration : IEntityTypeConfiguration<ComplaintStatusTransition>
{
    public void Configure(EntityTypeBuilder<ComplaintStatusTransition> b)
    {
        b.ToTable("complaint_status_transitions");
        b.Property(x => x.FromStatusCode).HasMaxLength(40);
        b.Property(x => x.ToStatusCode).HasMaxLength(40);
        b.HasIndex(x => new { x.FromStatusCode, x.ToStatusCode }).IsUnique();
        b.HasOne<ComplaintStatus>().WithMany().HasForeignKey(x => x.FromStatusCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ComplaintStatus>().WithMany().HasForeignKey(x => x.ToStatusCode).OnDelete(DeleteBehavior.Restrict);
        b.HasData(ReferenceSeed.Transitions);
    }
}

internal sealed class PriorityConfiguration : IEntityTypeConfiguration<ComplaintPriority>
{
    public void Configure(EntityTypeBuilder<ComplaintPriority> b)
    {
        b.ToTable("complaint_priorities");
        b.HasKey(x => x.Code);
        b.Property(x => x.Code).HasMaxLength(40);
        b.Property(x => x.Name).HasMaxLength(100);
        b.HasData(ReferenceSeed.Priorities);
    }
}

internal sealed class RoleMappingConfiguration : IEntityTypeConfiguration<ApplicationRoleMapping>
{
    public void Configure(EntityTypeBuilder<ApplicationRoleMapping> b)
    {
        b.ToTable("application_role_mapping");
        b.Property(x => x.IamRole).HasMaxLength(100);
        b.Property(x => x.OfficeType).HasMaxLength(50);
        b.Property(x => x.ApplicationRole).HasMaxLength(50);
        b.HasIndex(x => new { x.IamRole, x.OfficeType, x.ApplicationRole }).IsUnique().AreNullsDistinct(false);
        b.HasData(ReferenceSeed.RoleMappings);
    }
}

internal sealed class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> b)
    {
        b.ToTable("app_settings");
        b.HasKey(x => x.Key);
        b.Property(x => x.Key).HasMaxLength(100);
        b.Property(x => x.Value).HasMaxLength(1000);
        b.Property(x => x.UpdatedBy).HasMaxLength(32);
        b.HasData(ReferenceSeed.Settings);
    }
}
