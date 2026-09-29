namespace ComplaintManagement.Contracts.Responses;

public sealed record CategoryResponse(string Code, string Name, string Group, int? TatDays);
public sealed record StatusResponse(string Code, string Name, bool IsInitial, bool IsTerminal, int SortOrder);
public sealed record PriorityResponse(string Code, string Name, int Rank);
public sealed record RegionResponse(string Code, string Name);
public sealed record BranchResponse(string Code, string Name, string RegionCode);
public sealed record DepartmentResponse(string Code, string Name);
public sealed record EmployeeResponse(string EmployeeId, string Name, string? Designation, string? OfficeType, string? OfficeCode, string? OfficeName, string? DepartmentName);

/// <summary>The signed-in staff member as the portal sees them.</summary>
public sealed record CurrentUserResponse(
    string EmployeeId,
    string Name,
    string? Designation,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    /// <summary>HeadOffice, Region or Branch; null when the office type is not recognised.</summary>
    string? ScopeLevel,
    string? OfficeType,
    string? OfficeCode,
    string? OfficeName,
    string? DepartmentName);

/// <summary>Options for the public complaint form. Contains no internal configuration such as TAT.</summary>
public sealed record PublicFormOptions(IReadOnlyList<PublicCategory> Categories, IReadOnlyList<PublicBranch> Branches);
public sealed record PublicCategory(string Code, string Name, string Group);
public sealed record PublicBranch(string Code, string Name, string RegionName);

/// <summary>
/// Options for the staff "lodge a complaint" form. Branch staff get their branch in FixedBranch and no lists;
/// Regional and Head Office staff choose a region and a branch.
/// </summary>
public sealed record LodgeFormOptions(
    IReadOnlyList<PublicCategory> Categories,
    IReadOnlyList<RegionResponse> Regions,
    IReadOnlyList<BranchResponse> Branches,
    BranchResponse? FixedBranch,
    string Source);
