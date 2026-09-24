namespace ComplaintManagement.Application.Common;

public sealed class SlaOptions
{
    public const string SectionName = "Sla";

    /// <summary>How long before the due date a complaint is flagged as due soon. Provisional until the Bank sets it.</summary>
    public int WarningWindowHours { get; set; } = 48;
}

public static class SlaStates
{
    public const string NotSet = "NOT_SET";
    public const string OnTrack = "ON_TRACK";
    public const string DueSoon = "DUE_SOON";
    public const string Overdue = "OVERDUE";
    public const string Met = "MET";
    public const string Breached = "BREACHED";
}

public static class SlaCalculator
{
    public static DateTimeOffset? DueDate(DateTimeOffset receivedAt, int? tatDays) =>
        tatDays is > 0 ? receivedAt.AddDays(tatDays.Value) : null;

    public static (string State, int AgeDays, int? OverdueDays) Evaluate(
        DateTimeOffset createdAt,
        DateTimeOffset? dueDate,
        DateTimeOffset? closedAt,
        DateTimeOffset now,
        int warningWindowHours)
    {
        var end = closedAt ?? now;
        var ageDays = Math.Max(0, (int)Math.Floor((end - createdAt).TotalDays));

        if (dueDate is null) return (SlaStates.NotSet, ageDays, null);

        if (closedAt is not null)
        {
            return closedAt <= dueDate
                ? (SlaStates.Met, ageDays, null)
                : (SlaStates.Breached, ageDays, (int)Math.Ceiling((closedAt.Value - dueDate.Value).TotalDays));
        }

        if (now > dueDate) return (SlaStates.Overdue, ageDays, (int)Math.Ceiling((now - dueDate.Value).TotalDays));
        if (dueDate.Value - now <= TimeSpan.FromHours(warningWindowHours)) return (SlaStates.DueSoon, ageDays, null);
        return (SlaStates.OnTrack, ageDays, null);
    }
}
