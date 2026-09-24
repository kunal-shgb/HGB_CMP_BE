using ComplaintManagement.Application.Common;
using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Application.Common.Security;
using ComplaintManagement.Contracts.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ComplaintManagement.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardSummary> GetSummaryAsync(int trendDays, CancellationToken ct);
}

public sealed class DashboardService(
    IApplicationDbContext db,
    ICurrentUser user,
    TimeProvider clock,
    IOptions<SlaOptions> slaOptions) : IDashboardService
{
    private static readonly (string Code, string Name, int From, int To)[] AgeingBuckets =
    [
        ("0_7", "0-7 days", 0, 7),
        ("8_15", "8-15 days", 8, 15),
        ("16_30", "16-30 days", 16, 30),
        ("31_PLUS", "Over 30 days", 31, int.MaxValue),
    ];

    public async Task<DashboardSummary> GetSummaryAsync(int trendDays, CancellationToken ct)
    {
        trendDays = Math.Clamp(trendDays, 7, 90);
        var now = clock.GetUtcNow();
        var dueSoonLimit = now.AddHours(slaOptions.Value.WarningWindowHours);
        var scoped = db.Complaints.AsNoTracking().VisibleTo(user);
        var open = scoped.Where(c => c.ClosedAt == null);

        var statuses = await db.Statuses.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.SortOrder).ToListAsync(ct);
        var statusCounts = await scoped.GroupBy(c => c.StatusCode)
            .Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        var byStatus = statuses.Select(s => new StatusCount(s.Code, s.Name, s.IsTerminal, statusCounts.GetValueOrDefault(s.Code))).ToList();

        var total = statusCounts.Values.Sum();
        var pending = await open.CountAsync(ct);
        var overdue = await open.CountAsync(c => c.SlaDueDate != null && c.SlaDueDate < now, ct);
        var dueSoon = await open.CountAsync(c => c.SlaDueDate != null && c.SlaDueDate >= now && c.SlaDueDate <= dueSoonLimit, ct);

        // Trend is grouped by IST calendar day in memory; the window is bounded to at most 90 days.
        var since = IstDate.StartOfDayUtc(IstDate.ToIstDate(now).AddDays(-(trendDays - 1)));
        var received = await scoped.Where(c => c.CreatedAt >= since).Select(c => c.CreatedAt).ToListAsync(ct);
        var disposed = await scoped.Where(c => c.ClosedAt >= since).Select(c => c.ClosedAt!.Value).ToListAsync(ct);
        var receivedByDay = received.GroupBy(IstDate.ToIstDate).ToDictionary(g => g.Key, g => g.Count());
        var disposedByDay = disposed.GroupBy(IstDate.ToIstDate).ToDictionary(g => g.Key, g => g.Count());
        var firstDay = IstDate.ToIstDate(since);
        var trend = Enumerable.Range(0, trendDays).Select(i => firstDay.AddDays(i))
            .Select(d => new DailyCount(d, receivedByDay.GetValueOrDefault(d), disposedByDay.GetValueOrDefault(d)))
            .ToList();

        var byCategory = await scoped.GroupBy(c => new { c.Category!.Code, c.Category.Name })
            .OrderByDescending(g => g.Count())
            .Select(g => new NamedCount(g.Key.Code, g.Key.Name, g.Count()))
            .ToListAsync(ct);
        var byRegion = await scoped.GroupBy(c => new { c.Branch!.Region!.Code, c.Branch.Region.Name })
            .OrderByDescending(g => g.Count())
            .Select(g => new NamedCount(g.Key.Code, g.Key.Name, g.Count()))
            .ToListAsync(ct);

        var openCreated = await open.Select(c => c.CreatedAt).ToListAsync(ct);
        var ageing = AgeingBuckets.Select(b => new NamedCount(b.Code, b.Name,
            openCreated.Count(created =>
            {
                var age = (int)Math.Floor((now - created).TotalDays);
                return age >= b.From && age <= b.To;
            }))).ToList();

        return new DashboardSummary(total, pending, overdue, dueSoon, byStatus, trend, byCategory, byRegion, ageing);
    }
}
