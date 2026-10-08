using Microsoft.EntityFrameworkCore;
using PatentDocket.Api.Domain;
using PatentDocket.Api.Services;

namespace PatentDocket.Api.Data;

/// <summary>
/// Seeds an empty database with FICTIONAL demo data. Every client, inventor, title and
/// application number below is invented; none relates to a real person, company or
/// patent filing. Dates are relative to today so the dashboard always has something to show.
/// </summary>
public static class DemoDataSeeder
{
    public static async Task SeedAsync(DocketDbContext db, DocketClock clock, CancellationToken ct = default)
    {
        if (await db.Matters.AnyAsync(ct))
        {
            return;
        }

        var today = clock.Today;
        var now = clock.UtcNow;

        var attorneys = new List<Attorney>
        {
            new() { Name = "Jordan Example", Initials = "JE", Email = "jordan.example@example.com", Role = "Partner" },
            new() { Name = "Riley Sample", Initials = "RS", Email = "riley.sample@example.com", Role = "Associate" },
            new() { Name = "Casey Placeholder", Initials = "CP", Email = "casey.placeholder@example.com", Role = "Patent Agent" },
            new() { Name = "Morgan Demo", Initials = "MD", Email = "morgan.demo@example.com", Role = "Paralegal" },
        };
        db.Attorneys.AddRange(attorneys);

        Matter M(string docket, string title, string client, string jurisdiction, string? appNo, int filedDaysAgo, MatterStatus status, Attorney who) => new()
        {
            DocketNumber = docket,
            Title = title,
            ClientName = client,
            Jurisdiction = jurisdiction,
            ApplicationNumber = appNo,
            FilingDate = appNo is null ? null : today.AddDays(-filedDaysAgo),
            PriorityDate = appNo is null ? null : today.AddDays(-filedDaysAgo - 200),
            Status = status,
            ResponsibleAttorney = who,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        // Application numbers use the 99/ series, which the USPTO does not assign, to make them obviously fake.
        var matters = new List<Matter>
        {
            M("FAKE-2024-001-US", "Self-Calibrating Widget Torque Sensor", "Acme Widgets Ltd. (fictional)", "US", "99/100,001", 700, MatterStatus.Pending, attorneys[0]),
            M("FAKE-2024-002-US", "Low-Latency Mesh Routing for Toy Drones", "Northwind Robotics (fictional)", "US", "99/100,002", 640, MatterStatus.Pending, attorneys[1]),
            M("FAKE-2024-003-PCT", "Biodegradable Coffee Pod Lid", "Contoso Brewing Co. (fictional)", "PCT", "PCT/ZZ2025/000003", 260, MatterStatus.Pending, attorneys[0]),
            M("FAKE-2025-004-US", "Adaptive Glare Filter for Bicycle Lights", "Fabrikam Cycles (fictional)", "US", "99/100,004", 420, MatterStatus.Allowed, attorneys[2]),
            M("FAKE-2025-005-US", "Method of Folding Maps Back Correctly", "Tailspin Cartography (fictional)", "US", "99/100,005", 380, MatterStatus.Pending, attorneys[1]),
            M("FAKE-2025-006-EP", "Quiet Hinge for Office Doors", "Wingtip Hardware (fictional)", "EP", "EP99000006", 300, MatterStatus.Pending, attorneys[2]),
            M("FAKE-2026-007-US", "Plant Watering Reminder Using Soil Acoustics", "Acme Widgets Ltd. (fictional)", "US", null, 0, MatterStatus.Drafting, attorneys[0]),
            M("FAKE-2023-008-US", "Spill-Resistant Keyboard Membrane", "Northwind Robotics (fictional)", "US", "99/100,008", 1100, MatterStatus.Issued, attorneys[1]),
        };
        db.Matters.AddRange(matters);

        Deadline FromRule(Matter m, DeadlineType type, int triggerDaysAgo, string? notes = null, bool completed = false)
        {
            var calc = DeadlineRules.Calculate(type, today.AddDays(-triggerDaysAgo));
            return new Deadline
            {
                Matter = m,
                Type = type,
                Description = calc.Description,
                TriggerDate = calc.TriggerDate,
                DueDate = calc.DueDate,
                FinalDueDate = calc.FinalDueDate,
                Notes = notes,
                IsCompleted = completed,
                CompletedAtUtc = completed ? now.AddDays(-3) : null,
                CreatedAtUtc = now,
            };
        }

        Deadline Custom(Matter m, string description, int dueInDays, string? notes = null) => new()
        {
            Matter = m,
            Type = DeadlineType.Custom,
            Description = description,
            DueDate = today.AddDays(dueInDays),
            Notes = notes,
            CreatedAtUtc = now,
        };

        // Trigger offsets are chosen so due dates land overdue, this week, this month and later.
        db.Deadlines.AddRange(
            FromRule(matters[0], DeadlineType.NonFinalOfficeActionResponse, 88, "Examiner cites two references; draft amendments circulated."),
            FromRule(matters[1], DeadlineType.FinalOfficeActionResponse, 95, "Past the 3-month date: extension fee required. Confirm with client."),
            FromRule(matters[2], DeadlineType.PctNationalPhaseEntry, 30 * 29, "Client to confirm target countries (fictional: US, EP, JP)."),
            FromRule(matters[3], DeadlineType.IssueFeePayment, 75, "Not extendable."),
            FromRule(matters[4], DeadlineType.RestrictionRequirementResponse, 55),
            FromRule(matters[4], DeadlineType.InformationDisclosureStatement, 380, "Filed with the application.", completed: true),
            Custom(matters[5], "EPO Rule 161 response (entered manually)", 12, "Foreign associate handling; report due to client."),
            Custom(matters[6], "Inventor interview and draft review", 4),
            Custom(matters[7], "3.5-year maintenance fee window opens", 150),
            FromRule(matters[0], DeadlineType.InformationDisclosureStatement, 700, completed: true));

        await db.SaveChangesAsync(ct);
    }
}
