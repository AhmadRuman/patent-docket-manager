using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PatentDocket.Api.Contracts;
using PatentDocket.Api.Data;
using PatentDocket.Api.Domain;
using PatentDocket.Api.Services;

namespace PatentDocket.Api.Controllers;

[ApiController]
[Route("api/matters")]
public class MattersController(DocketDbContext db, DocketClock clock) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<MatterSummaryDto>> List(
        [FromQuery] string? search,
        [FromQuery] MatterStatus? status,
        [FromQuery] int? attorneyId,
        CancellationToken ct)
    {
        var query = db.Matters.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            // LIKE (unlike string.Contains) is case-insensitive on both SQL Server and SQLite.
            const string esc = "\\";
            var pattern = "%" + search.Trim().Replace(esc, esc + esc).Replace("%", esc + "%").Replace("[", esc + "[").Replace("_", esc + "_") + "%";
            query = query.Where(m =>
                EF.Functions.Like(m.DocketNumber, pattern, esc) ||
                EF.Functions.Like(m.Title, pattern, esc) ||
                EF.Functions.Like(m.ClientName, pattern, esc) ||
                (m.ApplicationNumber != null && EF.Functions.Like(m.ApplicationNumber, pattern, esc)));
        }
        if (status is not null)
        {
            query = query.Where(m => m.Status == status);
        }
        if (attorneyId is not null)
        {
            query = query.Where(m => m.ResponsibleAttorneyId == attorneyId);
        }

        var rows = await query
            .OrderBy(m => m.DocketNumber)
            .Select(m => new
            {
                Matter = m,
                m.ResponsibleAttorney,
                OpenCount = m.Deadlines.Count(d => !d.IsCompleted),
                NextDue = m.Deadlines.Where(d => !d.IsCompleted).Min(d => (DateOnly?)d.DueDate),
            })
            .ToListAsync(ct);

        return rows.Select(r => new MatterSummaryDto(
            r.Matter.Id,
            r.Matter.DocketNumber,
            r.Matter.Title,
            r.Matter.ClientName,
            r.Matter.Jurisdiction,
            r.Matter.ApplicationNumber,
            r.Matter.Status,
            r.ResponsibleAttorney?.ToDto(),
            r.OpenCount,
            r.NextDue)).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MatterDetailDto>> Get(int id, CancellationToken ct)
    {
        var matter = await LoadAsync(id, ct);
        return matter is null ? NotFound() : matter.ToDetailDto(clock.Today);
    }

    [HttpPost]
    public async Task<ActionResult<MatterDetailDto>> Create(MatterRequest request, CancellationToken ct)
    {
        if (await ValidateAsync(request, existingId: null, ct) is { } problem)
        {
            return problem;
        }

        var matter = new Matter
        {
            DocketNumber = "",
            Title = "",
            ClientName = "",
            Jurisdiction = "",
            CreatedAtUtc = clock.UtcNow,
            UpdatedAtUtc = clock.UtcNow,
        };
        request.Apply(matter);
        db.Matters.Add(matter);
        await db.SaveChangesAsync(ct);

        var created = (await LoadAsync(matter.Id, ct))!;
        return CreatedAtAction(nameof(Get), new { id = matter.Id }, created.ToDetailDto(clock.Today));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<MatterDetailDto>> Update(int id, MatterRequest request, CancellationToken ct)
    {
        var matter = await db.Matters.FindAsync([id], ct);
        if (matter is null)
        {
            return NotFound();
        }
        if (await ValidateAsync(request, existingId: id, ct) is { } problem)
        {
            return problem;
        }

        request.Apply(matter);
        matter.UpdatedAtUtc = clock.UtcNow;
        await db.SaveChangesAsync(ct);

        return (await LoadAsync(id, ct))!.ToDetailDto(clock.Today);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await db.Matters.Where(m => m.Id == id).ExecuteDeleteAsync(ct);
        return deleted == 0 ? NotFound() : NoContent();
    }

    private Task<Matter?> LoadAsync(int id, CancellationToken ct) =>
        db.Matters
            .AsNoTracking()
            .Include(m => m.ResponsibleAttorney)
            .Include(m => m.Deadlines)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

    private async Task<ActionResult?> ValidateAsync(MatterRequest request, int? existingId, CancellationToken ct)
    {
        var docket = request.DocketNumber.Trim();
        if (await db.Matters.AnyAsync(m => m.DocketNumber == docket && m.Id != existingId, ct))
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Duplicate docket number",
                detail: $"A matter with docket number '{docket}' already exists.");
        }
        if (request.ResponsibleAttorneyId is { } attorneyId && !await db.Attorneys.AnyAsync(a => a.Id == attorneyId, ct))
        {
            ModelState.AddModelError(nameof(request.ResponsibleAttorneyId), "Unknown attorney.");
            return ValidationProblem(ModelState);
        }
        return null;
    }
}
