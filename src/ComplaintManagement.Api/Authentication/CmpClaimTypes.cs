namespace ComplaintManagement.Api.Authentication;

/// <summary>
/// Normalised claim names used inside the portal. Whatever the Bank IAM issues is mapped onto
/// these by the IAM token handler, so the rest of the API never depends on IAM claim names.
/// </summary>
public static class CmpClaimTypes
{
    public const string EmployeeId = "cmp:employee_id";
    public const string Name = "cmp:name";
    public const string Designation = "cmp:designation";
    public const string IamRole = "cmp:iam_role";
    public const string AppRole = "cmp:app_role";
    public const string Region = "cmp:region";
    public const string Branch = "cmp:branch";
    public const string Department = "cmp:department";
}
