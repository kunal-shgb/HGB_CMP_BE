using ComplaintManagement.Application.Common.Interfaces;
using ComplaintManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ComplaintManagement.Application.Escalation;

/// <summary>Admin-managed automatic escalation rules, stored in app_settings.</summary>
public sealed record EscalationSettings(bool Enabled, int ToRegionalOfficeAfterDays, int ToHeadOfficeAfterDays)
{
    public static readonly EscalationSettings Default = new(true, 0, 7);

    public static async Task<EscalationSettings> LoadAsync(IApplicationDbContext db, CancellationToken ct)
    {
        string[] keys = [AppSettingKeys.EscalationEnabled, AppSettingKeys.EscalateToRegionalOfficeAfterDays, AppSettingKeys.EscalateToHeadOfficeAfterDays];
        var values = await db.AppSettings.AsNoTracking().Where(s => keys.Contains(s.Key)).ToDictionaryAsync(s => s.Key, s => s.Value, ct);
        return new EscalationSettings(
            bool.TryParse(values.GetValueOrDefault(AppSettingKeys.EscalationEnabled), out var enabled) ? enabled : Default.Enabled,
            int.TryParse(values.GetValueOrDefault(AppSettingKeys.EscalateToRegionalOfficeAfterDays), out var ro) ? ro : Default.ToRegionalOfficeAfterDays,
            int.TryParse(values.GetValueOrDefault(AppSettingKeys.EscalateToHeadOfficeAfterDays), out var ho) ? ho : Default.ToHeadOfficeAfterDays);
    }
}
