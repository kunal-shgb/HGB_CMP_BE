namespace ComplaintManagement.Contracts.Responses;

public sealed record AdminCategory(string Code, string Name, string Group, int SortOrder, bool IsActive, string? Description, IReadOnlyList<AdminSubCategory> SubCategories);
public sealed record AdminSubCategory(string Code, string Name, int? TatDays, string? DefaultPriorityCode, string? DefaultDepartmentCode, int SortOrder, bool IsActive);

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
