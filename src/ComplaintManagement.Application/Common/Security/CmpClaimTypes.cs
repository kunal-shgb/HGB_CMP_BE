namespace ComplaintManagement.Application.Common.Security;

/// <summary>
/// Normalised claim names used inside the portal. The IAM integration maps the Bank's user profile
/// onto these, so nothing outside Infrastructure/IAM depends on IAM field names.
/// </summary>
public static class CmpClaimTypes
{
    public const string EmployeeId = "cmp:employee_code";
    public const string Name = "cmp:name";
    public const string Designation = "cmp:designation";
    public const string IamRole = "cmp:access_role";
    public const string AppRole = "cmp:app_role";
    public const string OfficeType = "cmp:office_type";
    public const string OfficeCode = "cmp:office_code";
    public const string OfficeName = "cmp:office_name";
    public const string DepartmentName = "cmp:department";
}
