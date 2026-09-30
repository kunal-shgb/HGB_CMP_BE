using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Domain.Entities;
using ComplaintManagement.Domain.Enums;

namespace ComplaintManagement.Application.Common.Security;

/// <summary>
/// The Admin-defined flow of a complaint, taken from its category: which RO and HO division receives its
/// escalations and approvals, and whether a branch's escalations and approvals skip the RO. A category with
/// no route set sends everything to the whole office, as before.
/// </summary>
public static class ComplaintRouting
{
    /// <summary>The level an escalation from <paramref name="from"/> lands on for this category.</summary>
    public static int NextLevel(int from, ComplaintCategory? category) =>
        from < EscalationLevels.RegionalOffice && category?.DirectToHeadOffice == true
            ? EscalationLevels.HeadOffice
            : from + 1;

    /// <summary>The division (IAM department code) that handles this category at an escalation level, or null for the whole office.</summary>
    public static string? DivisionAt(int level, ComplaintCategory? category) => level switch
    {
        EscalationLevels.RegionalOffice => Blank(category?.RoDivisionCode),
        EscalationLevels.HeadOffice => Blank(category?.HoDivisionCode),
        _ => null,
    };

    /// <summary>The escalation level of the office the user works at, or null.</summary>
    public static int? LevelOf(ICurrentUser user) => user.ScopeLevel switch
    {
        ScopeLevel.Branch => EscalationLevels.Branch,
        ScopeLevel.Region => EscalationLevels.RegionalOffice,
        ScopeLevel.HeadOffice => EscalationLevels.HeadOffice,
        _ => null,
    };

    /// <summary>
    /// True when the complaint has been escalated to a division of the user's own office level and the user is
    /// not in it: they may still see the complaint, but their role grants no action on it.
    /// </summary>
    public static bool OutsideRoutedDivision(ICurrentUser user, Complaint complaint) =>
        complaint.EscalatedDivisionCode is not null
        && LevelOf(user) == complaint.EscalationLevel
        && !string.Equals(user.DepartmentName?.Trim(), complaint.EscalatedDivisionCode, StringComparison.OrdinalIgnoreCase);

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
