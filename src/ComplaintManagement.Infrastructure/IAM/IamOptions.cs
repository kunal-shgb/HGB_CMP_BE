using System.ComponentModel.DataAnnotations;

namespace ComplaintManagement.Infrastructure.IAM;

/// <summary>Bank IAM connection settings. Secrets come from user-secrets or environment variables only.</summary>
public sealed class IamOptions
{
    public const string SectionName = "IAM";

    [Url] public string? BaseUrl { get; set; }
    [Url] public string? Authority { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
}

/// <summary>
/// Development-only identities used by the dev token handler and dev directory. Contains no secrets.
/// Must never be enabled outside Development; the API refuses to start if it is.
/// </summary>
public sealed class DevIdentityOptions
{
    public const string SectionName = "DevAuth";

    public bool Enabled { get; set; }
    public List<DevIdentity> Users { get; set; } = [];
}

public sealed class DevIdentity
{
    [Required] public string EmployeeId { get; set; } = "";
    [Required] public string Name { get; set; } = "";
    public string? Email { get; set; }
    public string? Designation { get; set; }
    public List<string> IamRoles { get; set; } = [];
    public string? RegionCode { get; set; }
    public string? BranchCode { get; set; }
    public string? DepartmentCode { get; set; }
}
