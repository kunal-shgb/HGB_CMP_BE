using ComplaintManagement.Domain.Common;

namespace ComplaintManagement.Domain.Entities;

public class Complaint : AuditableEntity
{
    public required string ComplaintNumber { get; set; }

    // Customer
    public required string CustomerName { get; set; }
    public required string MobileNumber { get; set; }
    public string? Email { get; set; }
    public string? CustomerId { get; set; }
    public string? AccountNumber { get; set; }
    public string? PreferredChannel { get; set; }

    // Classification
    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }
    public Guid CategoryId { get; set; }
    public ComplaintCategory? Category { get; set; }
    public Guid SubCategoryId { get; set; }
    public ComplaintSubCategory? SubCategory { get; set; }
    public required string PriorityCode { get; set; }
    public ComplaintPriority? Priority { get; set; }
    public required string StatusCode { get; set; }
    public ComplaintStatus? Status { get; set; }

    // Complaint
    public required string Description { get; set; }
    public string? TransactionId { get; set; }
    public DateOnly? TransactionDate { get; set; }
    public decimal? TransactionAmount { get; set; }

    // Ownership
    public string? AssignedEmployeeId { get; set; }
    public string? AssignedEmployeeName { get; set; }
    public Guid? AssignedDepartmentId { get; set; }
    public Department? AssignedDepartment { get; set; }
    public int EscalationLevel { get; set; } = 1;

    // SLA / lifecycle
    public DateTimeOffset? SlaDueDate { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }

    public List<ComplaintStatusHistory> StatusHistory { get; set; } = [];
    public List<ComplaintAssignment> Assignments { get; set; } = [];
    public List<ComplaintRemark> Remarks { get; set; } = [];
    public List<ComplaintAttachment> Attachments { get; set; } = [];
}

public class ComplaintStatusHistory : Entity
{
    public Guid ComplaintId { get; set; }
    public string? OldStatusCode { get; set; }
    public required string NewStatusCode { get; set; }
    public string? Remarks { get; set; }
    /// <summary>Employee ID from the IAM context, or "CUSTOMER"/"SYSTEM" for non-staff actions.</summary>
    public required string ChangedByEmployeeId { get; set; }
    public string? ChangedByName { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
}

public class ComplaintAssignment : Entity
{
    public Guid ComplaintId { get; set; }
    public string? AssignedFromEmployeeId { get; set; }
    public required string AssignedToEmployeeId { get; set; }
    public string? AssignedToName { get; set; }
    public Guid? AssignedDepartmentId { get; set; }
    public string? Remarks { get; set; }
    public required string AssignedByEmployeeId { get; set; }
    public DateTimeOffset AssignedAt { get; set; }
}

public class ComplaintRemark : Entity
{
    public Guid ComplaintId { get; set; }
    public required string Remark { get; set; }
    public Enums.RemarkVisibility Visibility { get; set; }
    public required string CreatedByEmployeeId { get; set; }
    public string? CreatedByName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class ComplaintAttachment : Entity
{
    public Guid ComplaintId { get; set; }
    public required string FileName { get; set; }
    /// <summary>Storage key relative to the configured storage root. Never a web path.</summary>
    public required string StorageKey { get; set; }
    public required string ContentType { get; set; }
    public long FileSize { get; set; }
    public required string UploadedBy { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
}

public class AuditLog : Entity
{
    public required string EmployeeId { get; set; }
    public required string Action { get; set; }
    public required string Module { get; set; }
    public string? RecordId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Details { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
