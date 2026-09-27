using System.ComponentModel.DataAnnotations;

namespace ComplaintManagement.Infrastructure.IAM;

/// <summary>Bank IAM connection settings. Anything secret comes from user-secrets or environment variables only.</summary>
public sealed class IamOptions
{
    public const string SectionName = "IAM";

    /// <summary>Base URL of the Bank IAM API, e.g. https://iam.bank.internal/.</summary>
    [Url] public string? BaseUrl { get; set; }

    /// <summary>Path of the employee login endpoint, relative to BaseUrl. Confirm against the IAM API specification.</summary>
    public string LoginPath { get; set; } = "api/auth/login";

    [Range(2, 60)] public int TimeoutSeconds { get; set; } = 15;
}
