namespace ComplaintManagement.Domain.Entities;

/// <summary>A workflow setting managed by an Admin, e.g. which HO department checks HO Makers.</summary>
public class AppSetting
{
    public required string Key { get; set; }
    public string? Value { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public static class AppSettingKeys
{
    /// <summary>Department code (IAM departmentName) whose Checkers decide Head Office Makers' requests.</summary>
    public const string HeadOfficeMakerCheckerDepartment = "approvals.ho_maker_checker_department";

    /// <summary>"true" or "false": whether the background job escalates overdue complaints.</summary>
    public const string EscalationEnabled = "escalation.enabled";
    /// <summary>Whole days past the TAT due date before a complaint is escalated to its Regional Office.</summary>
    public const string EscalateToRegionalOfficeAfterDays = "escalation.to_ro_after_overdue_days";
    /// <summary>Whole days past the TAT due date before a complaint is escalated to Head Office.</summary>
    public const string EscalateToHeadOfficeAfterDays = "escalation.to_ho_after_overdue_days";

    /// <summary>Days after closure during which the customer may give feedback.</summary>
    public const string FeedbackWindowDays = "feedback.window_days";
}
