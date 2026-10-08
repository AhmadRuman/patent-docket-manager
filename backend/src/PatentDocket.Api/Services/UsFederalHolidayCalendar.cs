namespace PatentDocket.Api.Services;

/// <summary>
/// Business-day calendar for the USPTO. When a deadline falls on a Saturday, Sunday
/// or federal holiday in the District of Columbia, it rolls forward to the next
/// business day (35 U.S.C. 21(b)). Ad-hoc closures (e.g. inauguration day,
/// emergency closures) are not modelled and must be docketed manually.
/// </summary>
public static class UsFederalHolidayCalendar
{
    public static bool IsBusinessDay(DateOnly date) =>
        date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !IsFederalHoliday(date);

    public static DateOnly NextBusinessDayOnOrAfter(DateOnly date)
    {
        while (!IsBusinessDay(date))
        {
            date = date.AddDays(1);
        }
        return date;
    }

    public static bool IsFederalHoliday(DateOnly date) =>
        // A Saturday Jan 1 is observed on Friday Dec 31 of the prior year, so look one year ahead too.
        HolidaysObservedIn(date.Year).Contains(date) || HolidaysObservedIn(date.Year + 1).Contains(date);

    public static IReadOnlySet<DateOnly> HolidaysObservedIn(int year) => new HashSet<DateOnly>
    {
        Observed(new DateOnly(year, 1, 1)),             // New Year's Day
        NthWeekday(year, 1, DayOfWeek.Monday, 3),       // Birthday of Martin Luther King, Jr.
        NthWeekday(year, 2, DayOfWeek.Monday, 3),       // Washington's Birthday
        LastWeekday(year, 5, DayOfWeek.Monday),         // Memorial Day
        Observed(new DateOnly(year, 6, 19)),            // Juneteenth
        Observed(new DateOnly(year, 7, 4)),             // Independence Day
        NthWeekday(year, 9, DayOfWeek.Monday, 1),       // Labor Day
        NthWeekday(year, 10, DayOfWeek.Monday, 2),      // Columbus Day
        Observed(new DateOnly(year, 11, 11)),           // Veterans Day
        NthWeekday(year, 11, DayOfWeek.Thursday, 4),    // Thanksgiving Day
        Observed(new DateOnly(year, 12, 25)),           // Christmas Day
    };

    private static DateOnly Observed(DateOnly date) => date.DayOfWeek switch
    {
        DayOfWeek.Saturday => date.AddDays(-1),
        DayOfWeek.Sunday => date.AddDays(1),
        _ => date,
    };

    private static DateOnly NthWeekday(int year, int month, DayOfWeek day, int n)
    {
        var first = new DateOnly(year, month, 1);
        var offset = ((int)day - (int)first.DayOfWeek + 7) % 7;
        return first.AddDays(offset + 7 * (n - 1));
    }

    private static DateOnly LastWeekday(int year, int month, DayOfWeek day)
    {
        var last = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        var offset = ((int)last.DayOfWeek - (int)day + 7) % 7;
        return last.AddDays(-offset);
    }
}
