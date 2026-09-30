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
    IReadOnlyList<NamedCount> PendingAgeing,
    FeedbackSummary Feedback);

/// <summary>
/// Customer feedback received in the period (AverageRating null when none), and "not resolved" feedback still
/// awaiting staff review, whenever given.
/// </summary>
public sealed record FeedbackSummary(int Received, double? AverageRating, int NotResolved, int NeedsReview);

public sealed record StatusCount(string Code, string Name, bool IsTerminal, int Count);
public sealed record DailyCount(DateOnly Date, int Received, int Disposed);
public sealed record NamedCount(string Code, string Name, int Count);
