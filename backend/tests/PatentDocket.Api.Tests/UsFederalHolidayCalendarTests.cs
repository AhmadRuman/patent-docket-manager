using PatentDocket.Api.Services;

namespace PatentDocket.Api.Tests;

public class UsFederalHolidayCalendarTests
{
    [Theory]
    [InlineData("2026-01-01")] // New Year's Day (Thu)
    [InlineData("2026-01-19")] // MLK Day, 3rd Monday
    [InlineData("2026-02-16")] // Washington's Birthday, 3rd Monday
    [InlineData("2026-05-25")] // Memorial Day, last Monday
    [InlineData("2026-06-19")] // Juneteenth (Fri)
    [InlineData("2026-07-03")] // Independence Day falls on Saturday -> observed Friday
    [InlineData("2026-09-07")] // Labor Day
    [InlineData("2026-10-12")] // Columbus Day, 2nd Monday
    [InlineData("2026-11-11")] // Veterans Day (Wed)
    [InlineData("2026-11-26")] // Thanksgiving, 4th Thursday
    [InlineData("2026-12-25")] // Christmas (Fri)
    [InlineData("2027-12-31")] // New Year's Day 2028 is a Saturday -> observed Fri 31 Dec 2027
    [InlineData("2028-12-25")] // Christmas Monday
    public void Recognises_observed_federal_holidays(string date)
    {
        Assert.True(UsFederalHolidayCalendar.IsFederalHoliday(DateOnly.Parse(date)));
        Assert.False(UsFederalHolidayCalendar.IsBusinessDay(DateOnly.Parse(date)));
    }

    [Theory]
    [InlineData("2026-07-04")] // the Saturday itself is a weekend, not the observed holiday
    [InlineData("2026-10-13")]
    [InlineData("2026-03-17")]
    public void Ordinary_days_are_not_holidays(string date) =>
        Assert.False(UsFederalHolidayCalendar.IsFederalHoliday(DateOnly.Parse(date)));

    [Theory]
    [InlineData("2026-10-09", "2026-10-09")] // Friday stays
    [InlineData("2026-10-10", "2026-10-13")] // Saturday -> skips Sunday and Columbus Day
    [InlineData("2026-11-26", "2026-11-27")] // Thanksgiving -> Friday
    [InlineData("2026-12-25", "2026-12-28")] // Christmas Friday -> Monday
    public void Rolls_forward_to_next_business_day(string date, string expected) =>
        Assert.Equal(DateOnly.Parse(expected), UsFederalHolidayCalendar.NextBusinessDayOnOrAfter(DateOnly.Parse(date)));
}
