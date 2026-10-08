using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PatentDocket.Api.Contracts;
using PatentDocket.Api.Data;
using PatentDocket.Api.Domain;
using PatentDocket.Api.Services;

namespace PatentDocket.Api.Controllers;

public enum DeadlineStatusFilter
{
    Open,
    Completed,
    All,
}

[ApiController]
[Route("api")]
public class DeadlinesController(DocketDbContext db, DocketClock clock) : ControllerBase
{
    /// <summary>Lists deadlines, soonest first. Defaults to open deadlines only.</summary>
    [HttpGet("deadlines")]
    public async Task<IReadOnlyList<DeadlineDto>> List(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] DeadlineStatusFilter status = DeadlineStatusFilter.Open,
        [FromQuery] int? attorneyId = null,
        [FromQuery] int? matterId = null,
        CancellationToken ct = default)
    {
        var query = WithMatter();
        query = status switch
        {
            DeadlineStatusFilter.Open => query.Where(d => !d.IsCompleted),
            DeadlineStatusFilter.Completed => query.Where(d => d.IsCompleted),
            _ => query,
        };
        if (from is not null) query = query.Where(d => d.DueDate >= from);
        if (to is not null) query = query.Where(d => d.DueDate <= to);
        if (attorneyId is not null) query = query.Where(d => d.Matter!.ResponsibleAttorneyId == attorneyId);
        if (matterId is not null) query = query.Where(d => d.MatterId == matterId);

        var today = clock.Today;
        var rows = await query.OrderBy(d => d.DueDate).ThenBy(d => d.Id).ToListAsync(ct);
        return rows.Select(d => d.ToDto(today)).ToList();
    }

    [HttpGet("deadlines/{id:int}")]
    public async Task<ActionResult<DeadlineDto>> Get(int id, CancellationToken ct)
    {
        var deadline = await WithMatter().FirstOrDefaultAsync(d => d.Id == id, ct);
        return deadline is null ? NotFound() : deadline.ToDto(clock.Today);
    }

    [HttpPost("matters/{matterId:int}/deadlines")]
    public async Task<ActionResult<DeadlineDto>> Create(int matterId, DeadlineRequest request, CancellationToken ct)
    {
        if (!await db.Matters.AnyAsync(m => m.Id == matterId, ct))
        {
            return NotFound();
        }

        var deadline = new Deadline { MatterId = matterId, Description = "", CreatedAtUtc = clock.UtcNow };
        if (ApplyRequest(request, deadline) is { } problem)
        {
            return problem;
        }

        db.Deadlines.Add(deadline);
        await db.SaveChangesAsync(ct);

        var created = await WithMatter().FirstAsync(d => d.Id == deadline.Id, ct);
        return CreatedAtAction(nameof(Get), new { id = deadline.Id }, created.ToDto(clock.Today));
    }

    [HttpPut("deadlines/{id:int}")]
    public async Task<ActionResult<DeadlineDto>> Update(int id, DeadlineRequest request, CancellationToken ct)
    {
        var deadline = await db.Deadlines.Include(d => d.Matter).ThenInclude(m => m!.ResponsibleAttorney)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
        if (deadline is null)
        {
            return NotFound();
        }
        if (ApplyRequest(request, deadline) is { } problem)
        {
            return problem;
        }

        await db.SaveChangesAsync(ct);
        return deadline.ToDto(clock.Today);
    }

    [HttpPost("deadlines/{id:int}/complete")]
    public Task<ActionResult<DeadlineDto>> Complete(int id, CancellationToken ct) => SetCompleted(id, true, ct);

    [HttpPost("deadlines/{id:int}/reopen")]
    public Task<ActionResult<DeadlineDto>> Reopen(int id, CancellationToken ct) => SetCompleted(id, false, ct);

    [HttpDelete("deadlines/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await db.Deadlines.Where(d => d.Id == id).ExecuteDeleteAsync(ct);
        return deleted == 0 ? NotFound() : NoContent();
    }

    /// <summary>The automatic docketing rules the UI can offer.</summary>
    [HttpGet("deadline-rules")]
    public IEnumerable<DeadlineRuleDto> Rules() =>
        DeadlineRules.All.Select(r => new DeadlineRuleDto(r.Type, r.Label, r.MonthsFromTrigger, r.MaxMonthsWithExtensions, r.TriggerDescription));

    /// <summary>Previews the due dates a rule produces, without saving anything.</summary>
    [HttpGet("deadline-rules/calculate")]
    public ActionResult<CalculatedDeadlineDto> Calculate([FromQuery] DeadlineType type, [FromQuery] DateOnly triggerDate)
    {
        if (DeadlineRules.For(type) is null)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "No rule", detail: $"{type} deadlines must be entered manually.");
        }
        var c = DeadlineRules.Calculate(type, triggerDate);
        return new CalculatedDeadlineDto(c.Type, c.Description, c.TriggerDate, c.DueDate, c.FinalDueDate);
    }

    /// <summary>Overdue items, what is due in the next 7 days, and what follows in the next 30.</summary>
    [HttpGet("dashboard")]
    public async Task<DashboardDto> Dashboard(CancellationToken ct)
    {
        var today = clock.Today;
        var weekEnd = today.AddDays(Mapping.ThisWeekDays - 1);
        var horizon = today.AddDays(30);

        var open = await WithMatter()
            .Where(d => !d.IsCompleted && d.DueDate <= horizon)
            .OrderBy(d => d.DueDate)
            .ThenBy(d => d.Id)
            .ToListAsync(ct);
        var dtos = open.Select(d => d.ToDto(today)).ToList();

        var overdue = dtos.Where(d => d.DueDate < today).ToList();
        var thisWeek = dtos.Where(d => d.DueDate >= today && d.DueDate <= weekEnd).ToList();
        var upcoming = dtos.Where(d => d.DueDate > weekEnd).ToList();
        var activeMatters = await db.Matters.CountAsync(m => m.Status != MatterStatus.Abandoned && m.Status != MatterStatus.Issued, ct);

        return new DashboardDto(today, weekEnd, overdue.Count, thisWeek.Count, thisWeek.Count + upcoming.Count, activeMatters, overdue, thisWeek, upcoming);
    }

    private IQueryable<Deadline> WithMatter() =>
        db.Deadlines.AsNoTracking().Include(d => d.Matter).ThenInclude(m => m!.ResponsibleAttorney);

    private async Task<ActionResult<DeadlineDto>> SetCompleted(int id, bool completed, CancellationToken ct)
    {
        var deadline = await db.Deadlines.Include(d => d.Matter).ThenInclude(m => m!.ResponsibleAttorney)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
        if (deadline is null)
        {
            return NotFound();
        }

        deadline.IsCompleted = completed;
        deadline.CompletedAtUtc = completed ? clock.UtcNow : null;
        await db.SaveChangesAsync(ct);
        return deadline.ToDto(clock.Today);
    }

    /// <summary>Copies a request onto a deadline, calculating dates from the rule when no due date is given.</summary>
    private ActionResult? ApplyRequest(DeadlineRequest r, Deadline d)
    {
        var rule = DeadlineRules.For(r.Type);
        DateOnly due;
        DateOnly? final = r.FinalDueDate;

        if (r.DueDate is { } manual)
        {
            due = manual;
        }
        else if (rule is not null && r.TriggerDate is { } trigger)
        {
            var calc = DeadlineRules.Calculate(r.Type, trigger);
            due = calc.DueDate;
            final ??= calc.FinalDueDate;
        }
        else
        {
            ModelState.AddModelError(nameof(r.DueDate), rule is null
                ? "A due date is required for custom deadlines."
                : "Provide a due date, or a trigger date so the due date can be calculated.");
            return ValidationProblem(ModelState);
        }

        var description = string.IsNullOrWhiteSpace(r.Description) ? rule?.Label : r.Description.Trim();
        if (description is null)
        {
            ModelState.AddModelError(nameof(r.Description), "A description is required for custom deadlines.");
            return ValidationProblem(ModelState);
        }
        if (final is { } f && f < due)
        {
            ModelState.AddModelError(nameof(r.FinalDueDate), "The final (extended) due date cannot be before the due date.");
            return ValidationProblem(ModelState);
        }

        d.Type = r.Type;
        d.Description = description;
        d.TriggerDate = r.TriggerDate;
        d.DueDate = due;
        d.FinalDueDate = final;
        d.Notes = string.IsNullOrWhiteSpace(r.Notes) ? null : r.Notes.Trim();
        return null;
    }
}
