using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PatentDocket.Api.Contracts;
using PatentDocket.Api.Data;

namespace PatentDocket.Api.Controllers;

[ApiController]
[Route("api/attorneys")]
public class AttorneysController(DocketDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<AttorneyDto>> List(CancellationToken ct) =>
        (await db.Attorneys.AsNoTracking().OrderBy(a => a.Name).ToListAsync(ct)).Select(a => a.ToDto()).ToList();
}
