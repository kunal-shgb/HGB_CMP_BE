namespace ComplaintManagement.Application.Common.Interfaces;

/// <summary>
/// Organisation master data from the Bank IAM: Regional Offices, branches and departments.
/// The portal keeps no copy of these; implementations live in Infrastructure/IAM and are cached.
/// </summary>
public interface IIamOrganisationService
{
    Task<IReadOnlyList<IamRegion>> GetRegionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IamBranch>> GetBranchesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IamDepartment>> GetDepartmentsAsync(CancellationToken cancellationToken = default);
}

/// <summary>A Regional Office. Code equals the officeCode of RO users.</summary>
public sealed record IamRegion(string Code, string Name, bool IsActive);

/// <summary>A branch and the Regional Office it reports to. Code equals the officeCode of branch users.</summary>
public sealed record IamBranch(string Code, string Name, string RegionCode, string RegionName, bool IsActive);

/// <summary>A Head Office department. Code equals the departmentName in user profiles (e.g. "DBD").</summary>
public sealed record IamDepartment(string Code, string Name, bool IsActive);

public static class IamOrganisationExtensions
{
    public static async Task<IamBranch?> FindBranchAsync(this IIamOrganisationService org, string code, CancellationToken ct) =>
        (await org.GetBranchesAsync(ct)).FirstOrDefault(b => string.Equals(b.Code, code, StringComparison.OrdinalIgnoreCase));

    public static async Task<IamDepartment?> FindDepartmentAsync(this IIamOrganisationService org, string code, CancellationToken ct) =>
        (await org.GetDepartmentsAsync(ct)).FirstOrDefault(d => string.Equals(d.Code, code, StringComparison.OrdinalIgnoreCase));
}
