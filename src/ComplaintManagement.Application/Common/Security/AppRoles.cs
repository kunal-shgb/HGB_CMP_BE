using ComplaintManagement.Domain.Enums;

namespace ComplaintManagement.Application.Common.Security;

/// <summary>Application roles. IAM roles are mapped onto these through application_role_mapping.</summary>
public static class AppRoles
{
    public const string SuperAdmin = "SUPER_ADMIN";
    public const string HoAdmin = "HO_ADMIN";
    public const string HoDepartmentUser = "HO_DEPARTMENT_USER";
    public const string RegionalOfficeUser = "REGIONAL_OFFICE_USER";
    public const string BranchUser = "BRANCH_USER";
    public const string NodalOfficer = "NODAL_OFFICER";
    public const string Management = "MANAGEMENT";
    public const string Auditor = "AUDITOR";

    public static readonly IReadOnlyList<string> All =
        [SuperAdmin, HoAdmin, HoDepartmentUser, RegionalOfficeUser, BranchUser, NodalOfficer, Management, Auditor];

    /// <summary>The widest organisational scope granted by any of the user's roles.</summary>
    public static ScopeLevel ResolveScope(IEnumerable<string> roles)
    {
        var level = ScopeLevel.Branch;
        foreach (var role in roles)
        {
            var roleLevel = role switch
            {
                SuperAdmin or HoAdmin or NodalOfficer or Management or Auditor => ScopeLevel.HeadOffice,
                HoDepartmentUser => ScopeLevel.Department,
                RegionalOfficeUser => ScopeLevel.Region,
                _ => ScopeLevel.Branch,
            };
            if (roleLevel > level) level = roleLevel;
        }
        return level;
    }
}
