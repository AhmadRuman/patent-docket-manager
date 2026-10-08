using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PatentDocket.Api.Data;
using PatentDocket.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new() { Title = "Patent Docket API", Version = "v1", Description = "Demo API. All data is fictional." });
    o.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "PatentDocket.Api.xml"));
});

builder.Services.Configure<DocketOptions>(builder.Configuration.GetSection(DocketOptions.Section));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<DocketClock>();

var provider = builder.Configuration["Database:Provider"] ?? "SqlServer";
var connectionString = builder.Configuration.GetConnectionString("Docket")
    ?? throw new InvalidOperationException("Connection string 'Docket' is not configured.");
builder.Services.AddDbContext<DocketDbContext>(o =>
{
    if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        o.UseSqlite(connectionString);
    }
    else
    {
        // Retries cover transient faults, including an Azure SQL free-tier database resuming from auto-pause.
        o.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(maxRetryCount: 6, maxRetryDelay: TimeSpan.FromSeconds(20), errorNumbersToAdd: null));
    }
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

await InitializeDatabaseAsync(app, provider);

app.UseExceptionHandler();
app.UseStatusCodePages();
if (app.Configuration.GetValue("Swagger:Enabled", true))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors();

app.MapControllers();
app.MapGet("/health", async (DocketDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct) ? Results.Ok(new { status = "healthy" }) : Results.StatusCode(503))
    .ExcludeFromDescription();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();

static async Task InitializeDatabaseAsync(WebApplication app, string provider)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<DocketDbContext>();

    if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        // Migrations target SQL Server; the SQLite dev/test database is created from the model.
        await db.Database.EnsureCreatedAsync();
    }
    else if (app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", true))
    {
        await db.Database.MigrateAsync();
    }

    if (app.Configuration.GetValue("Database:SeedDemoData", false))
    {
        await DemoDataSeeder.SeedAsync(db, scope.ServiceProvider.GetRequiredService<DocketClock>());
    }
}

/// <summary>Exposed for WebApplicationFactory in the integration tests.</summary>
public partial class Program;
