namespace ComplaintManagement.Contracts.Requests;

/// <summary>A complete new order: every code of the list, first to last.</summary>
public sealed record ReorderRequest(IReadOnlyList<string> Codes);

public sealed record CreateCategoryGroupRequest(string Code, string Name, int? SortOrder);
public sealed record UpdateCategoryGroupRequest(string Name, int SortOrder, bool IsActive);

/// <summary>
/// RoDivisionCode / HoDivisionCode: the department that takes this category's escalations and approvals at the
/// Regional Office / Head Office (null: the whole office). DirectToHeadOffice: branch escalations and approvals skip the RO.
/// </summary>
public sealed record CreateCategoryRequest(string Code, string Name, string GroupCode, int? TatDays, string? DefaultPriorityCode,
    int? SortOrder, string? Description,
    string? RoDivisionCode = null, string? HoDivisionCode = null, bool DirectToHeadOffice = false);
public sealed record UpdateCategoryRequest(string Name, string GroupCode, int? TatDays, string? DefaultPriorityCode,
    int SortOrder, bool IsActive, string? Description,
    string? RoDivisionCode = null, string? HoDivisionCode = null, bool DirectToHeadOffice = false);


public sealed record UpdateStatusRequest(string Name, string CustomerLabel);
public sealed record CreateTransitionRequest(string FromStatusCode, string ToStatusCode, bool RequiresRemark, bool RequiresApproval);
public sealed record UpdateTransitionRequest(bool RequiresRemark, bool RequiresApproval, bool IsActive);

/// <summary>Null or empty clears the setting: any Head Office Checker may then decide HO Makers' requests.</summary>
public sealed record UpdateApprovalSettingsRequest(string? HeadOfficeMakerCheckerDepartment);

public sealed record UpdateEscalationSettingsRequest(bool Enabled, int ToRegionalOfficeAfterDays, int ToHeadOfficeAfterDays);

public sealed record UpdateFeedbackSettingsRequest(int WindowDays);
