using System.ComponentModel.DataAnnotations;

namespace ComplaintManagement.Api.Authentication;

/// <summary>
/// Settings for the portal token the API issues after the Bank IAM verifies an employee.
/// SigningKey is a secret: dotnet user-secrets locally, environment variables or the Bank's secret store elsewhere.
/// </summary>
public sealed class PortalTokenOptions
{
    public const string SectionName = "Auth";

    [Required, MinLength(32)] public string SigningKey { get; set; } = "";
    [Required] public string Issuer { get; set; } = "hgb-cmp-api";
    [Required] public string Audience { get; set; } = "hgb-cmp-portal";
    [Range(5, 720)] public int TokenLifetimeMinutes { get; set; } = 480;
}
