namespace ComplaintManagement.Application.Common;

/// <summary>Business dates are Indian Standard Time; storage is UTC.</summary>
public static class IstDate
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(5.5);

    public static DateTimeOffset StartOfDayUtc(DateOnly date) =>
        new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), Offset).ToUniversalTime();

    public static DateOnly ToIstDate(DateTimeOffset instant) => DateOnly.FromDateTime(instant.ToOffset(Offset).DateTime);
}
