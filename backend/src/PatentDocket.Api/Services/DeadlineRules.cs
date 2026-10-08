using PatentDocket.Api.Domain;

namespace PatentDocket.Api.Services;

public sealed record DeadlineRule(
    DeadlineType Type,
    string Label,
    int MonthsFromTrigger,
    int? MaxMonthsWithExtensions,
    string TriggerDescription);

public sealed record CalculatedDeadline(
    DeadlineType Type,
    string Description,
    DateOnly TriggerDate,
    DateOnly DueDate,
    DateOnly? FinalDueDate);

/// <summary>
/// Simplified docketing rules used to suggest due dates. These are illustrative
/// defaults for a demo application. They are not legal advice and do not replace
/// review of the actual office communication by a qualified docketing professional.
/// </summary>
public static class DeadlineRules
{
    public static readonly IReadOnlyList<DeadlineRule> All =
    [
        new(DeadlineType.NonFinalOfficeActionResponse, "Response to non-final office action", 3, 6, "Office action mailing date"),
        new(DeadlineType.FinalOfficeActionResponse, "Response to final office action", 3, 6, "Office action mailing date"),
        new(DeadlineType.RestrictionRequirementResponse, "Response to restriction requirement", 2, 6, "Restriction requirement mailing date"),
        new(DeadlineType.NoticeOfMissingParts, "Response to notice to file missing parts", 2, 7, "Notice mailing date"),
        new(DeadlineType.IssueFeePayment, "Issue fee payment", 3, null, "Notice of allowance mailing date"),
        new(DeadlineType.PctNationalPhaseEntry, "PCT national phase entry", 30, null, "Earliest priority date"),
        new(DeadlineType.InformationDisclosureStatement, "Information disclosure statement (3-month window)", 3, null, "Filing date or national stage entry"),
    ];

    public static DeadlineRule? For(DeadlineType type) => All.FirstOrDefault(r => r.Type == type);

    public static CalculatedDeadline Calculate(DeadlineType type, DateOnly triggerDate)
    {
        var rule = For(type) ?? throw new ArgumentException($"No automatic rule for {type}; enter the due date manually.", nameof(type));

        var due = UsFederalHolidayCalendar.NextBusinessDayOnOrAfter(AddMonths(triggerDate, rule.MonthsFromTrigger));
        DateOnly? final = rule.MaxMonthsWithExtensions is { } max
            ? UsFederalHolidayCalendar.NextBusinessDayOnOrAfter(AddMonths(triggerDate, max))
            : null;

        return new CalculatedDeadline(type, rule.Label, triggerDate, due, final);
    }

    /// <summary>
    /// Month periods end on the same day-of-month as the trigger; if that day does not
    /// exist (e.g. 30 Feb) the period ends on the last day of the month. DateOnly.AddMonths
    /// already clamps this way.
    /// </summary>
    private static DateOnly AddMonths(DateOnly date, int months) => date.AddMonths(months);
}
