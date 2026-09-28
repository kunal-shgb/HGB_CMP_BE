using ComplaintManagement.Application.Common.Interfaces;

namespace ComplaintManagement.Infrastructure.IAM;

/// <summary>User profile returned by the Bank IAM after an employee signs in with employee code and password.</summary>
public sealed class IamUserProfile
{
    public string Id { get; set; } = "";
    public string EmployeeCode { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? Designation { get; set; }
    /// <summary>e.g. "OfficeHead", "Maker", "Checker" or "NoRole".</summary>
    public string? AccessRole { get; set; }
    /// <summary>IAM system administrator; grants the portal's Admin role.</summary>
    public bool IsSystemAdmin { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int? OfficeId { get; set; }
    public string? OfficeCode { get; set; }
    public string? OfficeName { get; set; }
    /// <summary>"Head Office", "Regional Office" or "Branch".</summary>
    public string? OfficeType { get; set; }
    public string? Mobile { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset? LastLogin { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
}

public static class IamProfileMapper
{
    /// <summary>The fields the portal uses. The mobile number is deliberately dropped.</summary>
    public static IamUser ToIamUser(IamUserProfile p) =>
        new(p.EmployeeCode, p.FullName, p.Designation, p.AccessRole, p.OfficeType, p.OfficeCode, p.OfficeName, p.DepartmentName, p.IsActive, p.IsSystemAdmin);
}
