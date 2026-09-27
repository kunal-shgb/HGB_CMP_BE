using ComplaintManagement.Domain.Enums;

namespace ComplaintManagement.Application.Common.Security;

/// <summary>
/// Maps IAM officeType values to a visibility scope. Configured under "OfficeScopes" so the exact
/// strings the Bank IAM sends can be set without code changes. Unknown office types get no scope.
/// </summary>
public sealed class OfficeScopeOptions
{
    public const string SectionName = "OfficeScopes";

    public Dictionary<string, ScopeLevel> Map { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Head Office"] = ScopeLevel.HeadOffice,
        ["Regional Office"] = ScopeLevel.Region,
        ["Branch"] = ScopeLevel.Branch,
    };

    public ScopeLevel? Resolve(string? officeType)
    {
        if (string.IsNullOrWhiteSpace(officeType)) return null;
        // Config binding can replace the comparer; look up case-insensitively either way.
        foreach (var (key, level) in Map)
            if (string.Equals(key, officeType.Trim(), StringComparison.OrdinalIgnoreCase)) return level;
        return null;
    }
}
