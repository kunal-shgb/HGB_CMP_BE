using ComplaintManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ComplaintManagement.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Complaint> Complaints { get; }
    DbSet<ComplaintStatusHistory> ComplaintStatusHistory { get; }
    DbSet<ComplaintAssignment> ComplaintAssignments { get; }
    DbSet<ComplaintRemark> ComplaintRemarks { get; }
    DbSet<ComplaintAttachment> ComplaintAttachments { get; }
    DbSet<ComplaintCategory> Categories { get; }
    DbSet<ComplaintSubCategory> SubCategories { get; }
    DbSet<ComplaintStatus> Statuses { get; }
    DbSet<ComplaintStatusTransition> StatusTransitions { get; }
    DbSet<ComplaintPriority> Priorities { get; }
    DbSet<Region> Regions { get; }
    DbSet<Branch> Branches { get; }
    DbSet<Department> Departments { get; }
    DbSet<ApplicationRoleMapping> ApplicationRoleMappings { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
