namespace PatentDocket.Api.Services;

public sealed class DocketOptions
{
    public const string Section = "Docket";

    /// <summary>IANA time zone the firm dockets in. "Today" is evaluated in this zone.</summary>
    public string TimeZone { get; set; } = "America/New_York";
}

/// <summary>Answers "what is today's date for the docket", independent of server time zone.</summary>
public sealed class DocketClock(TimeProvider timeProvider, Microsoft.Extensions.Options.IOptions<DocketOptions> options)
{
    private readonly TimeZoneInfo _zone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone);

    public DateOnly Today =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), _zone).DateTime);

    public DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;
}
