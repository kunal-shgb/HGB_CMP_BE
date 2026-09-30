using ComplaintManagement.Domain.Common;

namespace ComplaintManagement.Domain.Entities;

/// <summary>A complaint moving up the escalation ladder, automatically (TAT breach) or by an employee.</summary>
public class ComplaintEscalation : Entity
{
    public Guid ComplaintId { get; set; }
    public int FromLevel { get; set; }
    public int ToLevel { get; set; }
    /// <summary>The division it was routed to, if the category names one.</summary>
    public string? ToDivisionCode { get; set; }
    public string? ToDivisionName { get; set; }
    public required string Reason { get; set; }
    /// <summary>Employee code, or "SYSTEM" for automatic escalation.</summary>
    public required string EscalatedBy { get; set; }
    public string? EscalatedByName { get; set; }
    public DateTimeOffset EscalatedAt { get; set; }
}

/// <summary>
/// Escalation levels from the product spec: 1 Branch / concerned office, 2 Regional Office, 3 Head Office.
/// (The spec's level 4, a nodal officer, has no role in the Bank's IAM, so Head Office is the top.)
/// </summary>
public static class EscalationLevels
{
    public const int Branch = 1;
    public const int RegionalOffice = 2;
    public const int HeadOffice = 3;
    public const int Max = HeadOffice;

    public static string Name(int level) => level switch
    {
        Branch => "Branch",
        RegionalOffice => "Regional Office",
        HeadOffice => "Head Office",
        _ => $"Level {level}",
    };
}
