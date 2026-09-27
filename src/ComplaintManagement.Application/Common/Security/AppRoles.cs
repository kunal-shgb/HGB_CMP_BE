namespace ComplaintManagement.Application.Common.Security;

/// <summary>
/// Application roles decide what a user may do. The Bank IAM accessRole is mapped onto these through
/// application_role_mapping. Which complaints a user sees comes from their office, not the role.
/// </summary>
public static class AppRoles
{
    /// <summary>Works complaints: updates status, assigns within the office, adds remarks, proposes decisions.</summary>
    public const string Maker = "MAKER";

    /// <summary>
    /// Approves or returns a Maker's decision and can assign complaints. Branch Makers are checked by their RO,
    /// RO Makers by HO, HO Makers by the designated HO department.
    /// </summary>
    public const string Checker = "CHECKER";

    /// <summary>Manages categories and workflow settings.</summary>
    public const string Admin = "ADMIN";

    public static readonly IReadOnlyList<string> All = [Maker, Checker, Admin];
}
