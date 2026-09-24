namespace ComplaintManagement.Contracts.Responses;

public sealed record StatusRef(string Code, string Name, bool IsTerminal);
public sealed record OrgRef(string Code, string Name);
public sealed record EmployeeRef(string EmployeeId, string? Name);

public sealed record SlaInfo(DateTimeOffset? DueDate, string State, int AgeDays, int? OverdueDays);

public sealed record ComplaintListItem(
    Guid Id,
    string ComplaintNumber,
    string CustomerName,
    string MobileMasked,
    OrgRef Branch,
    OrgRef Region,
    OrgRef Category,
    OrgRef SubCategory,
    StatusRef Status,
    OrgRef Priority,
    EmployeeRef? AssignedTo,
    SlaInfo Sla,
    DateTimeOffset CreatedAt);

public sealed record ComplaintDetail(
    Guid Id,
    string ComplaintNumber,
    CustomerInfo Customer,
    TransactionInfo Transaction,
    string Description,
    OrgRef Branch,
    OrgRef Region,
    OrgRef Category,
    OrgRef SubCategory,
    StatusRef Status,
    OrgRef Priority,
    EmployeeRef? AssignedTo,
    OrgRef? AssignedDepartment,
    int EscalationLevel,
    SlaInfo Sla,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ResolvedAt,
    DateTimeOffset? ClosedAt,
    IReadOnlyList<AllowedTransition> AllowedTransitions,
    IReadOnlyList<RemarkItem> Remarks,
    IReadOnlyList<AttachmentItem> Attachments);

/// <summary>Customer identifiers are masked unless the caller may see them in full.</summary>
public sealed record CustomerInfo(
    string Name,
    string Mobile,
    string? Email,
    string? CustomerId,
    string? AccountNumber,
    string? PreferredChannel,
    bool IsMasked);

public sealed record TransactionInfo(string? TransactionId, DateOnly? TransactionDate, decimal? Amount);

public sealed record AllowedTransition(string Code, string Name, bool RequiresRemark);

public sealed record RemarkItem(Guid Id, string Remark, string Visibility, EmployeeRef CreatedBy, DateTimeOffset CreatedAt);

public sealed record AttachmentItem(Guid Id, string FileName, string ContentType, long FileSize, DateTimeOffset UploadedAt);

public sealed record TimelineEvent(
    DateTimeOffset At,
    string Type,
    string Title,
    string? Detail,
    EmployeeRef? By);

public sealed record CreateComplaintResponse(bool Success, string ComplaintNumber, string Message);
