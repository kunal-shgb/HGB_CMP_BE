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
    int EscalationLevel,
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
    /// <summary>Whether the caller may escalate this complaint one level up now.</summary>
    bool CanEscalate,
    SlaInfo Sla,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ResolvedAt,
    DateTimeOffset? ClosedAt,
    IReadOnlyList<AllowedTransition> AllowedTransitions,
    PendingApprovalInfo? PendingApproval,
    IReadOnlyList<RemarkItem> Remarks,
    IReadOnlyList<AttachmentItem> Attachments,
    ComplaintAbilities Abilities);

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

/// <summary>A status move the caller may make. RequiresApproval moves go to a Checker instead of applying at once.</summary>
public sealed record AllowedTransition(string Code, string Name, bool RequiresRemark, bool RequiresApproval);

/// <summary>A Maker's decision waiting for a Checker.</summary>
public sealed record PendingApprovalInfo(
    Guid Id,
    StatusRef RequestedStatus,
    EmployeeRef RequestedBy,
    string? RequestedByOffice,
    DateTimeOffset RequestedAt,
    string? Remarks,
    /// <summary>Region or HeadOffice.</summary>
    string ApproverLevel,
    string? ApproverOfficeCode,
    /// <summary>HO department whose Checkers decide, for Head Office Makers' requests.</summary>
    string? ApproverDepartment,
    bool CanDecide);

public sealed record ApprovalListItem(
    Guid ApprovalId,
    Guid ComplaintId,
    string ComplaintNumber,
    string CustomerName,
    OrgRef Branch,
    OrgRef Category,
    StatusRef RequestedStatus,
    EmployeeRef RequestedBy,
    string? RequestedByOffice,
    DateTimeOffset RequestedAt,
    string? Remarks,
    SlaInfo Sla);

public sealed record ChangeStatusResponse(bool PendingApproval, string Message);

public sealed record RemarkItem(Guid Id, string Remark, string Visibility, EmployeeRef CreatedBy, DateTimeOffset CreatedAt);

/// <summary>UploadedBy.EmployeeId is "CUSTOMER" for documents lodged with the complaint. ScanStatus: CLEAN or NOT_SCANNED.</summary>
public sealed record AttachmentItem(Guid Id, string FileName, string ContentType, long FileSize, DateTimeOffset UploadedAt, EmployeeRef UploadedBy, string ScanStatus);

public sealed record TimelineEvent(
    DateTimeOffset At,
    string Type,
    string Title,
    string? Detail,
    EmployeeRef? By);

public sealed record CreateComplaintResponse(bool Success, string ComplaintNumber, string Message);

/// <summary>A message sent (or queued) to the customer. The recipient is always masked.</summary>
public sealed record NotificationItem(
    Guid Id, string Event, string Channel, string RecipientMasked, string? Subject, string Body,
    string Status, int Attempts, DateTimeOffset CreatedAt, DateTimeOffset? SentAt, string? LastError);

/// <summary>
/// Always the same, whether or not the details matched, so the endpoint cannot be used to discover complaints.
/// DevelopmentOtp is filled only in Development, until an SMS gateway exists.
/// </summary>
public sealed record TrackingOtpResponse(string Message, int ExpiresInMinutes, string? DevelopmentOtp = null);

/// <summary>What a customer may see about their complaint. No internal remarks, staff or assignment details.</summary>
public sealed record TrackingView(
    string ComplaintNumber,
    string Status,
    string Category,
    string SubCategory,
    string Branch,
    DateTimeOffset RegisteredAt,
    DateTimeOffset LastUpdatedAt,
    DateTimeOffset? ResolvedAt,
    DateTimeOffset? ClosedAt,
    IReadOnlyList<TrackingUpdate> Updates);

/// <summary>A status change as the customer sees it, or a remark staff marked visible to the customer.</summary>
public sealed record TrackingUpdate(DateTimeOffset At, string Title, string? Detail);

/// <summary>What the signed-in user may do on this complaint (roles plus assignment). The API enforces the same rules.</summary>
public sealed record ComplaintAbilities(
    bool ChangeStatus,
    bool AddRemark,
    bool AddAttachment,
    bool Assign,
    bool Escalate,
    bool ViewCustomerDetails,
    bool IsAssignedToMe);
