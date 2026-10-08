using System.ComponentModel.DataAnnotations;
using PatentDocket.Api.Domain;

namespace PatentDocket.Api.Contracts;

public sealed record AttorneyDto(int Id, string Name, string Initials, string Email, string Role);

public sealed record DeadlineDto(
    int Id,
    int MatterId,
    string DocketNumber,
    string MatterTitle,
    string ClientName,
    string? ResponsibleAttorneyInitials,
    DeadlineType Type,
    string Description,
    DateOnly? TriggerDate,
    DateOnly DueDate,
    DateOnly? FinalDueDate,
    string? Notes,
    bool IsCompleted,
    DateTime? CompletedAtUtc,
    int DaysUntilDue,
    string Urgency);

public sealed record MatterSummaryDto(
    int Id,
    string DocketNumber,
    string Title,
    string ClientName,
    string Jurisdiction,
    string? ApplicationNumber,
    MatterStatus Status,
    AttorneyDto? ResponsibleAttorney,
    int OpenDeadlineCount,
    DateOnly? NextDueDate);

public sealed record MatterDetailDto(
    int Id,
    string DocketNumber,
    string Title,
    string ClientName,
    string Jurisdiction,
    string? ApplicationNumber,
    DateOnly? FilingDate,
    DateOnly? PriorityDate,
    MatterStatus Status,
    AttorneyDto? ResponsibleAttorney,
    IReadOnlyList<DeadlineDto> Deadlines,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed class MatterRequest
{
    [Required, StringLength(40, MinimumLength = 3)]
    public string DocketNumber { get; set; } = "";

    [Required, StringLength(300, MinimumLength = 3)]
    public string Title { get; set; } = "";

    [Required, StringLength(200, MinimumLength = 2)]
    public string ClientName { get; set; } = "";

    [Required, StringLength(10, MinimumLength = 2)]
    public string Jurisdiction { get; set; } = "US";

    [StringLength(40)]
    public string? ApplicationNumber { get; set; }

    public DateOnly? FilingDate { get; set; }
    public DateOnly? PriorityDate { get; set; }
    public MatterStatus Status { get; set; } = MatterStatus.Pending;
    public int? ResponsibleAttorneyId { get; set; }
}

/// <summary>
/// Either give a <see cref="DueDate"/> directly, or give a rule-based <see cref="Type"/>
/// plus <see cref="TriggerDate"/> and the server calculates the due dates.
/// </summary>
public sealed class DeadlineRequest
{
    public DeadlineType Type { get; set; } = DeadlineType.Custom;

    [StringLength(300)]
    public string? Description { get; set; }

    public DateOnly? TriggerDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateOnly? FinalDueDate { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}

public sealed record DeadlineRuleDto(DeadlineType Type, string Label, int MonthsFromTrigger, int? MaxMonthsWithExtensions, string TriggerDescription);

public sealed record CalculatedDeadlineDto(DeadlineType Type, string Description, DateOnly TriggerDate, DateOnly DueDate, DateOnly? FinalDueDate);

public sealed record DashboardDto(
    DateOnly Today,
    DateOnly ThisWeekEnd,
    int OverdueCount,
    int DueThisWeekCount,
    int DueNext30DaysCount,
    int ActiveMatterCount,
    IReadOnlyList<DeadlineDto> Overdue,
    IReadOnlyList<DeadlineDto> ThisWeek,
    IReadOnlyList<DeadlineDto> Upcoming);
