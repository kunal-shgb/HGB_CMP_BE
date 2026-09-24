namespace ComplaintManagement.Application.Common.Security;

/// <summary>
/// Named permissions. Each one is registered as an ASP.NET Core policy of the same name.
/// The role matrix below is provisional until the Bank confirms it.
/// </summary>
public static class Permissions
{
    public const string ComplaintView = "Complaint.View";
    public const string ComplaintViewUnmasked = "Complaint.ViewUnmasked";
    public const string ComplaintAssign = "Complaint.Assign";
    public const string ComplaintChangeStatus = "Complaint.ChangeStatus";
    public const string ComplaintAddRemark = "Complaint.AddRemark";
    public const string ComplaintEscalate = "Complaint.Escalate";
    public const string DashboardView = "Dashboard.View";
    public const string ReportView = "Report.View";
    public const string AdminManage = "Admin.Manage";

    public static readonly IReadOnlyList<string> All =
    [
        ComplaintView, ComplaintViewUnmasked, ComplaintAssign, ComplaintChangeStatus,
        ComplaintAddRemark, ComplaintEscalate, DashboardView, ReportView, AdminManage,
    ];

    private static readonly Dictionary<string, string[]> RoleMatrix = new()
    {
        [AppRoles.SuperAdmin] = [ComplaintView, DashboardView, ReportView, AdminManage],
        [AppRoles.HoAdmin] = [ComplaintView, ComplaintViewUnmasked, ComplaintAssign, ComplaintChangeStatus, ComplaintAddRemark, ComplaintEscalate, DashboardView, ReportView],
        [AppRoles.HoDepartmentUser] = [ComplaintView, ComplaintViewUnmasked, ComplaintAssign, ComplaintChangeStatus, ComplaintAddRemark, ComplaintEscalate, DashboardView],
        [AppRoles.RegionalOfficeUser] = [ComplaintView, ComplaintViewUnmasked, ComplaintAssign, ComplaintChangeStatus, ComplaintAddRemark, ComplaintEscalate, DashboardView, ReportView],
        [AppRoles.BranchUser] = [ComplaintView, ComplaintViewUnmasked, ComplaintChangeStatus, ComplaintAddRemark, ComplaintEscalate, DashboardView],
        [AppRoles.NodalOfficer] = [ComplaintView, ComplaintViewUnmasked, ComplaintAssign, ComplaintChangeStatus, ComplaintAddRemark, ComplaintEscalate, DashboardView, ReportView],
        [AppRoles.Management] = [ComplaintView, DashboardView, ReportView],
        [AppRoles.Auditor] = [ComplaintView, ComplaintViewUnmasked, DashboardView, ReportView],
    };

    public static IReadOnlySet<string> ForRoles(IEnumerable<string> roles) =>
        roles.SelectMany(r => RoleMatrix.TryGetValue(r, out var p) ? p : []).ToHashSet(StringComparer.Ordinal);
}
