namespace ComplaintManagement.Contracts.Responses;

public sealed record DashboardSummary(
    int Total,
    int Pending,
    int Overdue,
    int DueSoon,
    /// <summary>Open complaints escalated to Regional Office or Head Office.</summary>
    int Escalated,
    IReadOnlyList<StatusCount> ByStatus,
    IReadOnlyList<DailyCount> DailyTrend,
    IReadOnlyList<NamedCount> ByCategory,
    IReadOnlyList<NamedCount> ByRegion,
    IReadOnlyList<NamedCount> PendingAgeing);

public sealed record StatusCount(string Code, string Name, bool IsTerminal, int Count);
public sealed record DailyCount(DateOnly Date, int Received, int Disposed);
public sealed record NamedCount(string Code, string Name, int Count);
