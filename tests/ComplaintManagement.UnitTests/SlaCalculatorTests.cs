using ComplaintManagement.Application.Common;

namespace ComplaintManagement.UnitTests;

public class SlaCalculatorTests
{
    private static readonly DateTimeOffset Created = new(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Due_date_is_null_when_tat_not_configured() => Assert.Null(SlaCalculator.DueDate(Created, null));

    [Fact]
    public void Due_date_adds_tat_days() => Assert.Equal(Created.AddDays(7), SlaCalculator.DueDate(Created, 7));

    [Theory]
    [InlineData(1, SlaStates.OnTrack)]
    [InlineData(6, SlaStates.DueSoon)]
    [InlineData(8, SlaStates.Overdue)]
    public void Open_complaint_state_depends_on_time_to_due(int daysAfterCreation, string expected)
    {
        var (state, age, _) = SlaCalculator.Evaluate(Created, Created.AddDays(7), null, Created.AddDays(daysAfterCreation), 48);
        Assert.Equal(expected, state);
        Assert.Equal(daysAfterCreation, age);
    }

    [Fact]
    public void Overdue_reports_days_past_due()
    {
        var (_, _, overdue) = SlaCalculator.Evaluate(Created, Created.AddDays(7), null, Created.AddDays(9).AddHours(1), 48);
        Assert.Equal(3, overdue);
    }

    [Fact]
    public void Closed_on_time_is_met_and_late_is_breached()
    {
        Assert.Equal(SlaStates.Met, SlaCalculator.Evaluate(Created, Created.AddDays(7), Created.AddDays(5), Created.AddDays(30), 48).State);
        Assert.Equal(SlaStates.Breached, SlaCalculator.Evaluate(Created, Created.AddDays(7), Created.AddDays(8), Created.AddDays(30), 48).State);
    }

    [Fact]
    public void No_due_date_is_not_set() =>
        Assert.Equal(SlaStates.NotSet, SlaCalculator.Evaluate(Created, null, null, Created.AddDays(3), 48).State);
}
