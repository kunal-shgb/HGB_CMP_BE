namespace ComplaintManagement.Application.Common.Security;

/// <summary>Named permissions. Each one is registered as an ASP.NET Core policy of the same name.</summary>
public static class Permissions
{
    public const string ComplaintView = "Complaint.View";
    /// <summary>Lodge a complaint on a customer's behalf. Every employee may.</summary>
    public const string ComplaintCreate = "Complaint.Create";
    public const string ComplaintViewUnmasked = "Complaint.ViewUnmasked";
    public const string ComplaintAssign = "Complaint.Assign";
    public const string ComplaintChangeStatus = "Complaint.ChangeStatus";
    public const string ComplaintAddRemark = "Complaint.AddRemark";
    public const string ComplaintAddAttachment = "Complaint.AddAttachment";
    public const string ComplaintApprove = "Complaint.Approve";
    public const string ComplaintEscalate = "Complaint.Escalate";
    public const string DashboardView = "Dashboard.View";
    public const string ReportView = "Report.View";
    /// <summary>Categories and workflow settings.</summary>
    public const string AdminManage = "Admin.Manage";

    public static readonly IReadOnlyList<string> All =
    [
        ComplaintView, ComplaintCreate, ComplaintViewUnmasked, ComplaintAssign, ComplaintChangeStatus,
        ComplaintAddRemark, ComplaintAddAttachment, ComplaintApprove, ComplaintEscalate, DashboardView, ReportView, AdminManage,
    ];

    private static readonly Dictionary<string, string[]> RoleMatrix = new()
    {
        [AppRoles.Maker] = [ComplaintView, ComplaintViewUnmasked, ComplaintAssign, ComplaintChangeStatus, ComplaintAddRemark, ComplaintAddAttachment, ComplaintEscalate, DashboardView, ReportView],
        [AppRoles.Checker] = [ComplaintView, ComplaintViewUnmasked, ComplaintApprove, ComplaintAssign, ComplaintAddRemark, ComplaintAddAttachment, ComplaintEscalate, DashboardView, ReportView],
        [AppRoles.OfficeHead] = [ComplaintView, ComplaintViewUnmasked, ComplaintAssign, ComplaintChangeStatus, ComplaintAddRemark, ComplaintAddAttachment, ComplaintEscalate, DashboardView, ReportView],
        [AppRoles.Viewer] = [ComplaintView, ComplaintCreate, DashboardView],
        [AppRoles.Admin] = [AdminManage, ComplaintView, DashboardView, ReportView],
    };

    public static IReadOnlySet<string> ForRoles(IEnumerable<string> roles) =>
        roles.SelectMany(r => RoleMatrix.TryGetValue(r, out var p) ? p : []).ToHashSet(StringComparer.Ordinal);
}
