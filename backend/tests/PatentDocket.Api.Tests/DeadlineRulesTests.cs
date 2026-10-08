using PatentDocket.Api.Domain;
using PatentDocket.Api.Services;

namespace PatentDocket.Api.Tests;

public class DeadlineRulesTests
{
    [Fact]
    public void Non_final_office_action_is_three_months_extendable_to_six()
    {
        var c = DeadlineRules.Calculate(DeadlineType.NonFinalOfficeActionResponse, new DateOnly(2026, 3, 3));

        Assert.Equal(new DateOnly(2026, 6, 3), c.DueDate);
        Assert.Equal(new DateOnly(2026, 9, 3), c.FinalDueDate);
        Assert.Equal("Response to non-final office action", c.Description);
    }

    [Fact]
    public void Month_end_trigger_clamps_to_last_day_of_shorter_month()
    {
        // 30 Nov + 3 months = 28 Feb 2027 (a Sunday) -> Monday 1 Mar.
        var c = DeadlineRules.Calculate(DeadlineType.IssueFeePayment, new DateOnly(2026, 11, 30));

        Assert.Equal(new DateOnly(2027, 3, 1), c.DueDate);
        Assert.Null(c.FinalDueDate);
    }

    [Fact]
    public void Due_date_on_a_holiday_rolls_forward()
    {
        // 12 Jul + 3 months = 12 Oct 2026, Columbus Day.
        var c = DeadlineRules.Calculate(DeadlineType.FinalOfficeActionResponse, new DateOnly(2026, 7, 12));

        Assert.Equal(new DateOnly(2026, 10, 13), c.DueDate);
    }

    [Fact]
    public void Pct_national_phase_is_thirty_months_from_priority()
    {
        var c = DeadlineRules.Calculate(DeadlineType.PctNationalPhaseEntry, new DateOnly(2025, 1, 15));

        Assert.Equal(new DateOnly(2027, 7, 15), c.DueDate);
        Assert.Null(c.FinalDueDate);
    }

    [Fact]
    public void Custom_deadlines_have_no_rule() =>
        Assert.Throws<ArgumentException>(() => DeadlineRules.Calculate(DeadlineType.Custom, new DateOnly(2026, 1, 1)));

    [Fact]
    public void Every_non_custom_type_has_a_rule()
    {
        var missing = Enum.GetValues<DeadlineType>()
            .Where(t => t != DeadlineType.Custom && DeadlineRules.For(t) is null);
        Assert.Empty(missing);
    }
}
