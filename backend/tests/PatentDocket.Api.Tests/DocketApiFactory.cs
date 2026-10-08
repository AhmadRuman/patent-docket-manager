using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace PatentDocket.Api.Tests;

/// <summary>Fixed clock: Wednesday 7 October 2026, 14:00 UTC (10:00 in New York).</summary>
public sealed class FixedTimeProvider() : TimeProvider
{
    public static readonly DateTimeOffset Now = new(2026, 10, 7, 14, 0, 0, TimeSpan.Zero);
    public static readonly DateOnly Today = new(2026, 10, 7);

    public override DateTimeOffset GetUtcNow() => Now;
}

/// <summary>Runs the real API in-process against a throwaway SQLite database.</summary>
public sealed class DocketApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"patentdocket-test-{Guid.NewGuid():N}.db");

    public bool SeedDemoData { get; init; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Database:Provider", "Sqlite");
        builder.UseSetting("ConnectionStrings:Docket", $"Data Source={_dbPath}");
        builder.UseSetting("Database:SeedDemoData", SeedDemoData.ToString());
        builder.ConfigureServices(services => services.Replace(ServiceDescriptor.Singleton<TimeProvider>(new FixedTimeProvider())));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        File.Delete(_dbPath);
    }
}
