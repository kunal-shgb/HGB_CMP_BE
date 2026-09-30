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

    // Intake. See ComplaintSources. Staff-lodged complaints record who lodged them and from which office.
    public string Source { get; set; } = ComplaintSources.Website;
    public string? LodgedByEmployeeId { get; set; }
    public string? LodgedByName { get; set; }
    public string? LodgedByOfficeName { get; set; }

    // Office the complaint belongs to, as held by the Bank IAM when it was registered.
    // Organisation master data lives in the IAM; the portal keeps this snapshot for scoping and reporting.
    public required string BranchCode { get; set; }
    public required string BranchName { get; set; }
    public required string RegionCode { get; set; }
    public required string RegionName { get; set; }

    // Classification
    public Guid CategoryId { get; set; }
    public ComplaintCategory? Category { get; set; }
    public required string PriorityCode { get; set; }
    public ComplaintPriority? Priority { get; set; }
    public required string StatusCode { get; set; }
    public ComplaintStatus? Status { get; set; }

    // Complaint
    /// <summary>Short summary given by the customer or the staff member lodging it.</summary>
    public required string Title { get; set; }
    public required string Description { get; set; }
    public string? TransactionId { get; set; }
    public DateOnly? TransactionDate { get; set; }
    public decimal? TransactionAmount { get; set; }

    // Ownership
    public string? AssignedEmployeeId { get; set; }
    public string? AssignedEmployeeName { get; set; }
    /// <summary>IAM department code (e.g. "DBD") and its name at the time of assignment.</summary>
    public string? AssignedDepartmentCode { get; set; }
    public string? AssignedDepartmentName { get; set; }
    /// <summary>See <see cref="EscalationLevels"/>. Only ever goes up.</summary>
    public int EscalationLevel { get; set; } = EscalationLevels.Branch;
    /// <summary>
    /// The division (IAM department) the complaint was escalated to at its current level, and its name at the time.
    /// While set, only that division's staff at that office act on it. Null: the whole office.
    /// </summary>
    public string? EscalatedDivisionCode { get; set; }
    public string? EscalatedDivisionName { get; set; }

    // SLA / lifecycle
    public DateTimeOffset? SlaDueDate { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }

    public List<ComplaintStatusHistory> StatusHistory { get; set; } = [];
    public List<ComplaintAssignment> Assignments { get; set; } = [];
    public List<ComplaintRemark> Remarks { get; set; } = [];
    public List<ComplaintAttachment> Attachments { get; set; } = [];
    public List<ComplaintEscalation> Escalations { get; set; } = [];
    public List<ComplaintFeedback> Feedback { get; set; } = [];
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
    public string? AssignedDepartmentCode { get; set; }
    public string? Remarks { get; set; }
    public required string AssignedByEmployeeId { get; set; }
    public DateTimeOffset AssignedAt { get; set; }
}

public class ComplaintRemark : Entity
{
    public Guid ComplaintId { get; set; }
    /// <summary>Files added together with this remark.</summary>
    public List<ComplaintAttachment> Attachments { get; set; } = [];
    public required string Remark { get; set; }
    public Enums.RemarkVisibility Visibility { get; set; }
    public required string CreatedByEmployeeId { get; set; }
    public string? CreatedByName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class ComplaintAttachment : Entity
{
    public Guid ComplaintId { get; set; }
    /// <summary>The remark the file was added with; null for documents lodged with the complaint.</summary>
    public Guid? RemarkId { get; set; }
    public required string FileName { get; set; }
    /// <summary>Storage key relative to the configured storage root. Never a web path.</summary>
    public required string StorageKey { get; set; }
    /// <summary>Content type detected from the file's bytes, never the one the client sent.</summary>
    public required string ContentType { get; set; }
    public long FileSize { get; set; }
    /// <summary>SHA-256 of the stored bytes (hex), for integrity checks.</summary>
    public required string Sha256 { get; set; }
    /// <summary>Result of the malware scan: CLEAN, or NOT_SCANNED while no scanning engine is configured.</summary>
    public required string ScanStatus { get; set; }
    /// <summary>Employee code, or "CUSTOMER" for documents lodged with the complaint.</summary>
    public required string UploadedBy { get; set; }
    public string? UploadedByName { get; set; }
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
