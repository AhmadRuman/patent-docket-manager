using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PatentDocket.Api.Contracts;
using PatentDocket.Api.Domain;

namespace PatentDocket.Api.Tests;

public class ApiTests : IDisposable
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private readonly DocketApiFactory _factory = new();
    private readonly HttpClient _client;

    public ApiTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    private async Task<MatterDetailDto> CreateMatterAsync(string docket = "FAKE-T-001", string title = "Test Widget")
    {
        var response = await _client.PostAsJsonAsync("/api/matters", new MatterRequest
        {
            DocketNumber = docket,
            Title = title,
            ClientName = "Test Client (fictional)",
            Jurisdiction = "us",
        }, Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<MatterDetailDto>(Json))!;
    }

    private async Task<DeadlineDto> AddDeadlineAsync(int matterId, DeadlineRequest request)
    {
        var response = await _client.PostAsJsonAsync($"/api/matters/{matterId}/deadlines", request, Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<DeadlineDto>(Json))!;
    }

    [Fact]
    public async Task Creates_and_reads_a_matter()
    {
        var created = await CreateMatterAsync();

        Assert.Equal("US", created.Jurisdiction);
        var fetched = await _client.GetFromJsonAsync<MatterDetailDto>($"/api/matters/{created.Id}", Json);
        Assert.Equal("FAKE-T-001", fetched!.DocketNumber);
    }

    [Fact]
    public async Task Rejects_duplicate_docket_numbers()
    {
        await CreateMatterAsync("FAKE-DUP");

        var response = await _client.PostAsJsonAsync("/api/matters",
            new MatterRequest { DocketNumber = "FAKE-DUP", Title = "Another", ClientName = "Other" }, Json);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Rejects_invalid_matter()
    {
        var response = await _client.PostAsJsonAsync("/api/matters", new MatterRequest { DocketNumber = "", Title = "x" }, Json);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Search_is_case_insensitive_and_treats_wildcards_literally()
    {
        await CreateMatterAsync("FAKE-A", "Quiet Hinge");
        await CreateMatterAsync("FAKE-B", "Loud_Hinge");

        var quiet = await _client.GetFromJsonAsync<List<MatterSummaryDto>>("/api/matters?search=quiet", Json);
        var underscore = await _client.GetFromJsonAsync<List<MatterSummaryDto>>("/api/matters?search=d_h", Json);

        Assert.Equal("FAKE-A", Assert.Single(quiet!).DocketNumber);
        Assert.Equal("FAKE-B", Assert.Single(underscore!).DocketNumber);
    }

    [Fact]
    public async Task Calculates_due_dates_from_rule_and_trigger_date()
    {
        var matter = await CreateMatterAsync();

        var deadline = await AddDeadlineAsync(matter.Id, new DeadlineRequest
        {
            Type = DeadlineType.NonFinalOfficeActionResponse,
            TriggerDate = new DateOnly(2026, 7, 12),
        });

        Assert.Equal("Response to non-final office action", deadline.Description);
        Assert.Equal(new DateOnly(2026, 10, 13), deadline.DueDate); // rolled past Columbus Day
        Assert.Equal(new DateOnly(2027, 1, 12), deadline.FinalDueDate);
        Assert.Equal(6, deadline.DaysUntilDue);
        Assert.Equal("thisWeek", deadline.Urgency);
    }

    [Fact]
    public async Task Custom_deadline_requires_due_date_and_description()
    {
        var matter = await CreateMatterAsync();

        var noDate = await _client.PostAsJsonAsync($"/api/matters/{matter.Id}/deadlines", new DeadlineRequest { Description = "Call client" }, Json);
        var noDescription = await _client.PostAsJsonAsync($"/api/matters/{matter.Id}/deadlines", new DeadlineRequest { DueDate = FixedTimeProvider.Today }, Json);

        Assert.Equal(HttpStatusCode.BadRequest, noDate.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, noDescription.StatusCode);
    }

    [Fact]
    public async Task Completing_a_deadline_removes_it_from_the_open_list()
    {
        var matter = await CreateMatterAsync();
        var deadline = await AddDeadlineAsync(matter.Id, new DeadlineRequest { Description = "Call client", DueDate = FixedTimeProvider.Today });

        var completed = await (await _client.PostAsync($"/api/deadlines/{deadline.Id}/complete", null)).Content.ReadFromJsonAsync<DeadlineDto>(Json);
        var open = await _client.GetFromJsonAsync<List<DeadlineDto>>("/api/deadlines", Json);
        var all = await _client.GetFromJsonAsync<List<DeadlineDto>>("/api/deadlines?status=All", Json);

        Assert.True(completed!.IsCompleted);
        Assert.Equal("completed", completed.Urgency);
        Assert.Empty(open!);
        Assert.Single(all!);

        var reopened = await (await _client.PostAsync($"/api/deadlines/{deadline.Id}/reopen", null)).Content.ReadFromJsonAsync<DeadlineDto>(Json);
        Assert.False(reopened!.IsCompleted);
        Assert.Null(reopened.CompletedAtUtc);
    }

    [Fact]
    public async Task Dashboard_buckets_deadlines_by_urgency()
    {
        var matter = await CreateMatterAsync();
        var today = FixedTimeProvider.Today;
        await AddDeadlineAsync(matter.Id, new DeadlineRequest { Description = "Overdue", DueDate = today.AddDays(-1) });
        await AddDeadlineAsync(matter.Id, new DeadlineRequest { Description = "Today", DueDate = today });
        await AddDeadlineAsync(matter.Id, new DeadlineRequest { Description = "Day 6", DueDate = today.AddDays(6) });
        await AddDeadlineAsync(matter.Id, new DeadlineRequest { Description = "Day 7", DueDate = today.AddDays(7) });
        await AddDeadlineAsync(matter.Id, new DeadlineRequest { Description = "Far", DueDate = today.AddDays(90) });

        var dashboard = await _client.GetFromJsonAsync<DashboardDto>("/api/dashboard", Json);

        Assert.Equal(today, dashboard!.Today);
        Assert.Equal(["Overdue"], dashboard.Overdue.Select(d => d.Description));
        Assert.Equal(["Today", "Day 6"], dashboard.ThisWeek.Select(d => d.Description));
        Assert.Equal(["Day 7"], dashboard.Upcoming.Select(d => d.Description));
        Assert.Equal(3, dashboard.DueNext30DaysCount);
    }

    [Fact]
    public async Task Deleting_a_matter_deletes_its_deadlines()
    {
        var matter = await CreateMatterAsync();
        var deadline = await AddDeadlineAsync(matter.Id, new DeadlineRequest { Description = "X", DueDate = FixedTimeProvider.Today });

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/matters/{matter.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/deadlines/{deadline.Id}")).StatusCode);
    }

    [Fact]
    public async Task Calculate_endpoint_previews_without_saving()
    {
        var preview = await _client.GetFromJsonAsync<CalculatedDeadlineDto>(
            "/api/deadline-rules/calculate?type=IssueFeePayment&triggerDate=2026-11-30", Json);

        Assert.Equal(new DateOnly(2027, 3, 1), preview!.DueDate);
        Assert.Empty((await _client.GetFromJsonAsync<List<DeadlineDto>>("/api/deadlines?status=All", Json))!);
    }
}

public class DemoDataTests : IDisposable
{
    private readonly DocketApiFactory _factory = new() { SeedDemoData = true };

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Seeds_only_clearly_fictional_data_with_something_due_this_week()
    {
        var client = _factory.CreateClient();
        var json = new JsonSerializerOptions(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

        var matters = await client.GetFromJsonAsync<List<MatterSummaryDto>>("/api/matters", json);
        var attorneys = await client.GetFromJsonAsync<List<AttorneyDto>>("/api/attorneys", json);
        var dashboard = await client.GetFromJsonAsync<DashboardDto>("/api/dashboard", json);

        Assert.NotEmpty(matters!);
        Assert.All(matters!, m =>
        {
            Assert.StartsWith("FAKE-", m.DocketNumber);
            Assert.EndsWith("(fictional)", m.ClientName);
        });
        Assert.All(attorneys!, a => Assert.EndsWith("@example.com", a.Email));
        Assert.NotEmpty(dashboard!.Overdue);
        Assert.NotEmpty(dashboard.ThisWeek);
        Assert.NotEmpty(dashboard.Upcoming);
    }
}
