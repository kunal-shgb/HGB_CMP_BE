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

    /// <summary>
    /// Branch head (IAM accessRole "OfficeHead" at a Branch). The only branch role that works complaints;
    /// the people they assign a complaint to can work that complaint too.
    /// </summary>
    public const string OfficeHead = "OFFICE_HEAD";

    /// <summary>Every signed-in employee: can see complaints in their office scope (customer details masked).</summary>
    public const string Viewer = "VIEWER";

    /// <summary>Manages categories and workflow settings. Granted by the IAM's isSystemAdmin flag.</summary>
    public const string Admin = "ADMIN";

    public static readonly IReadOnlyList<string> All = [Maker, Checker, OfficeHead, Viewer, Admin];
}
