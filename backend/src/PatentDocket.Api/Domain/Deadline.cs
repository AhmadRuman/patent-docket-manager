namespace PatentDocket.Api.Domain;

public enum DeadlineType
{
    NonFinalOfficeActionResponse,
    FinalOfficeActionResponse,
    RestrictionRequirementResponse,
    NoticeOfMissingParts,
    IssueFeePayment,
    PctNationalPhaseEntry,
    InformationDisclosureStatement,
    Custom,
}

/// <summary>A dated obligation on a matter, such as an office action response.</summary>
public class Deadline
{
    public int Id { get; set; }

    public int MatterId { get; set; }
    public Matter? Matter { get; set; }

    public DeadlineType Type { get; set; }
    public required string Description { get; set; }

    /// <summary>The event the deadline is computed from (e.g. office action mailing date).</summary>
    public DateOnly? TriggerDate { get; set; }

    /// <summary>The date the response is due without paying for extensions.</summary>
    public DateOnly DueDate { get; set; }

    /// <summary>The last possible date with all available extensions, if extendable.</summary>
    public DateOnly? FinalDueDate { get; set; }

    public string? Notes { get; set; }

    public bool IsCompleted { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
