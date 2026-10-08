namespace PatentDocket.Api.Domain;

public enum MatterStatus
{
    Drafting,
    Pending,
    Allowed,
    Issued,
    Abandoned,
}

/// <summary>A patent matter (one application or family member) tracked by the docket.</summary>
public class Matter
{
    public int Id { get; set; }

    /// <summary>Firm-internal reference, e.g. "PDM-2026-0042-US". Unique.</summary>
    public required string DocketNumber { get; set; }

    public required string Title { get; set; }
    public required string ClientName { get; set; }

    /// <summary>Patent office, e.g. "US", "EP", "PCT".</summary>
    public required string Jurisdiction { get; set; }

    public string? ApplicationNumber { get; set; }
    public DateOnly? FilingDate { get; set; }
    public DateOnly? PriorityDate { get; set; }
    public MatterStatus Status { get; set; } = MatterStatus.Pending;

    public int? ResponsibleAttorneyId { get; set; }
    public Attorney? ResponsibleAttorney { get; set; }

    public List<Deadline> Deadlines { get; set; } = [];

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
