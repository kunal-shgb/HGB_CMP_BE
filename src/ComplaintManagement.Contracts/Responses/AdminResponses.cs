namespace ComplaintManagement.Contracts.Responses;

/// <summary>Everything on the admin categories screen. Counts tell the UI what can be deleted.</summary>
public sealed record AdminCategoryCatalogue(IReadOnlyList<AdminCategoryGroup> Groups, IReadOnlyList<AdminCategory> Categories);

/// <summary>A group can be deleted only when no category belongs to it.</summary>
public sealed record AdminCategoryGroup(string Code, string Name, int SortOrder, bool IsActive, int CategoryCount);

/// <summary>A category can be deleted only while no complaint uses it (ComplaintCount = 0).</summary>
public sealed record AdminCategory(string Code, string Name, string GroupCode, string Group, int? TatDays, string? DefaultPriorityCode,
    string? DefaultDepartmentCode, int SortOrder, bool IsActive, string? Description, int ComplaintCount);

public sealed record AdminStatus(
    string Code, string Name, string CustomerLabel, bool IsInitial, bool IsTerminal, bool IsResolution,
    bool IsAssignment, bool IsApprovalPending, int SortOrder, bool IsActive);

public sealed record AdminTransition(int Id, string FromStatusCode, string ToStatusCode, bool RequiresRemark, bool RequiresApproval, bool IsActive);

public sealed record ApprovalSettings(string? HeadOfficeMakerCheckerDepartment);

/// <summary>Automatic escalation: days past the TAT due date before moving to RO, then HO.</summary>
public sealed record EscalationSettingsDto(bool Enabled, int ToRegionalOfficeAfterDays, int ToHeadOfficeAfterDays);

public sealed record AdminWorkflow(
    IReadOnlyList<AdminStatus> Statuses,
    IReadOnlyList<AdminTransition> Transitions,
    ApprovalSettings Approvals,
    EscalationSettingsDto Escalation,
    IReadOnlyList<DepartmentResponse> Departments,
    IReadOnlyList<PriorityResponse> Priorities);
