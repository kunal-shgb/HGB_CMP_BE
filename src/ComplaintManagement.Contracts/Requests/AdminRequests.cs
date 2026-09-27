namespace ComplaintManagement.Contracts.Requests;

public sealed record CreateCategoryRequest(string Code, string Name, string Group, int? SortOrder, string? Description);
public sealed record UpdateCategoryRequest(string Name, string Group, int SortOrder, bool IsActive, string? Description);

public sealed record CreateSubCategoryRequest(string Code, string Name, int? TatDays, string? DefaultPriorityCode, string? DefaultDepartmentCode, int? SortOrder);
public sealed record UpdateSubCategoryRequest(string Name, int? TatDays, string? DefaultPriorityCode, string? DefaultDepartmentCode, int SortOrder, bool IsActive);

public sealed record UpdateStatusRequest(string Name, string CustomerLabel);
public sealed record CreateTransitionRequest(string FromStatusCode, string ToStatusCode, bool RequiresRemark, bool RequiresApproval);
public sealed record UpdateTransitionRequest(bool RequiresRemark, bool RequiresApproval, bool IsActive);

/// <summary>Null or empty clears the setting: any Head Office Checker may then decide HO Makers' requests.</summary>
public sealed record UpdateApprovalSettingsRequest(string? HeadOfficeMakerCheckerDepartment);

public sealed record UpdateEscalationSettingsRequest(bool Enabled, int ToRegionalOfficeAfterDays, int ToHeadOfficeAfterDays);
