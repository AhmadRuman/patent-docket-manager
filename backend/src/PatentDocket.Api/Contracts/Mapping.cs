using PatentDocket.Api.Domain;

namespace PatentDocket.Api.Contracts;

public static class Mapping
{
    /// <summary>Deadlines due within this many days (inclusive of today) count as "this week".</summary>
    public const int ThisWeekDays = 7;

    public static AttorneyDto ToDto(this Attorney a) => new(a.Id, a.Name, a.Initials, a.Email, a.Role);

    /// <remarks>Requires <see cref="Deadline.Matter"/> (and its attorney) to be loaded.</remarks>
    public static DeadlineDto ToDto(this Deadline d, DateOnly today)
    {
        var days = d.DueDate.DayNumber - today.DayNumber;
        return new DeadlineDto(
            d.Id,
            d.MatterId,
            d.Matter?.DocketNumber ?? "",
            d.Matter?.Title ?? "",
            d.Matter?.ClientName ?? "",
            d.Matter?.ResponsibleAttorney?.Initials,
            d.Type,
            d.Description,
            d.TriggerDate,
            d.DueDate,
            d.FinalDueDate,
            d.Notes,
            d.IsCompleted,
            d.CompletedAtUtc,
            days,
            Urgency(d.IsCompleted, days));
    }

    public static string Urgency(bool isCompleted, int daysUntilDue) => isCompleted switch
    {
        true => "completed",
        _ when daysUntilDue < 0 => "overdue",
        _ when daysUntilDue == 0 => "today",
        _ when daysUntilDue < ThisWeekDays => "thisWeek",
        _ when daysUntilDue <= 30 => "soon",
        _ => "later",
    };

    public static MatterDetailDto ToDetailDto(this Matter m, DateOnly today) => new(
        m.Id,
        m.DocketNumber,
        m.Title,
        m.ClientName,
        m.Jurisdiction,
        m.ApplicationNumber,
        m.FilingDate,
        m.PriorityDate,
        m.Status,
        m.ResponsibleAttorney?.ToDto(),
        m.Deadlines
            .OrderBy(d => d.IsCompleted)
            .ThenBy(d => d.DueDate)
            .Select(d => { d.Matter = m; return d.ToDto(today); })
            .ToList(),
        m.CreatedAtUtc,
        m.UpdatedAtUtc);

    public static void Apply(this MatterRequest r, Matter m)
    {
        m.DocketNumber = r.DocketNumber.Trim();
        m.Title = r.Title.Trim();
        m.ClientName = r.ClientName.Trim();
        m.Jurisdiction = r.Jurisdiction.Trim().ToUpperInvariant();
        m.ApplicationNumber = string.IsNullOrWhiteSpace(r.ApplicationNumber) ? null : r.ApplicationNumber.Trim();
        m.FilingDate = r.FilingDate;
        m.PriorityDate = r.PriorityDate;
        m.Status = r.Status;
        m.ResponsibleAttorneyId = r.ResponsibleAttorneyId;
    }
}
