using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;
using WeighingMS.Infrastructure.Access;
using WeighingMS.Infrastructure.Persistence;

[assembly: AssemblyFixture(typeof(WeighingMS.Api.IntegrationTests.ApiFixture))]

// One shared database, reset before each test: integration tests run one at a time.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]

namespace WeighingMS.Api.IntegrationTests;

/// <summary>
/// One PostgreSQL 17 container per test run (Testcontainers), migrated once. Each test resets the data with
/// Respawn and re-seeds the lab users. The Api runs in the <c>Test</c> environment over HTTPS (Secure cookies)
/// with a <see cref="FakeTimeProvider"/>.
/// </summary>
public sealed class ApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();
    private Respawner? _respawner;

    private static readonly DateTimeOffset _start = new(2026, 11, 3, 21, 48, 22, TimeSpan.Zero);

    /// <summary>
    /// The controllable clock for the current test. A new one is created on every reset; Api hosts pick it up when
    /// they start (after the reset), so tests never need to move time backwards.
    /// </summary>
    public FakeTimeProvider Clock { get; private set; } = new(_start);

    /// <summary>The database connection string.</summary>
    public string ConnectionString => _postgres.GetConnectionString();

    /// <summary>The lab users seeded before every test.</summary>
    public static IReadOnlyList<SeedUserOptions> SeedUsers { get; } =
    [
        new() { BadgeId = "OP-1001", DisplayName = "R. Karim", Role = "Operator" },
        new() { BadgeId = "OP-1002", DisplayName = "S. Akter", Role = "Operator" },
        new() { BadgeId = "SV-2001", DisplayName = "M. Rahman", Role = "Supervisor", Pin = "2468" },
    ];

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        await using var db = NewDbContext();
        await db.Database.MigrateAsync();
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            TablesToIgnore = ["__EFMigrationsHistory"],
        });
    }

    /// <summary>Empties every table, re-seeds the lab users and resets the clock.</summary>
    public async Task ResetAsync()
    {
        await using (var connection = new NpgsqlConnection(ConnectionString))
        {
            await connection.OpenAsync();
            await _respawner!.ResetAsync(connection);
        }

        await using var db = NewDbContext();
        var seeder = new DevelopmentUserSeeder(db, new PinHasher(), Microsoft.Extensions.Logging.Abstractions.NullLogger<DevelopmentUserSeeder>.Instance);
        await seeder.SeedAsync(SeedUsers, CancellationToken.None);
        Clock = new FakeTimeProvider(_start);
    }

    /// <summary>A DbContext on the test database, for assertions.</summary>
    public WeighingDbContext NewDbContext() =>
        new(new DbContextOptionsBuilder<WeighingDbContext>().UseNpgsql(ConnectionString).UseSnakeCaseNamingConvention().Options);

    /// <summary>Creates an Api host on the test database. <paramref name="settings"/> overrides configuration.</summary>
    public WebApplicationFactory<Program> CreateApi(IDictionary<string, string?>? settings = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Test");
            b.UseSetting("ConnectionStrings:Db", ConnectionString);
            b.UseSetting("RateLimits:AuthPermitLimitPerMinute", "1000");
            foreach (var (key, value) in settings ?? new Dictionary<string, string?>())
            {
                b.UseSetting(key, value);
            }

            b.ConfigureTestServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(_ => Clock);
            });
        });

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await _postgres.DisposeAsync();
}

/// <summary>An HTTPS client with a cookie jar that sends the XSRF token on state-changing requests.</summary>
public sealed class StationClient : IDisposable
{
    private static readonly Uri _base = new("https://localhost");
    private readonly CookieContainer _cookies = new();

    /// <summary>Creates a client for an Api host.</summary>
    public StationClient(WebApplicationFactory<Program> api)
    {
        ArgumentNullException.ThrowIfNull(api);
        Http = api.CreateDefaultClient(_base, new CookieContainerHandler(_cookies));
    }

    /// <summary>The underlying client (no XSRF header unless added).</summary>
    public HttpClient Http { get; }

    /// <summary>The current XSRF-TOKEN cookie value, if any.</summary>
    public string? XsrfToken => _cookies.GetCookies(_base)["XSRF-TOKEN"]?.Value;

    /// <summary>The cookies held for the Api.</summary>
    public CookieCollection Cookies => _cookies.GetCookies(_base);

    /// <summary>Fetches a fresh antiforgery token.</summary>
    public async Task PrimeAntiforgeryAsync() =>
        (await Http.GetAsync(new Uri("/api/antiforgery", UriKind.Relative), TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

    /// <summary>Sends a request with the current XSRF token.</summary>
    public Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (body is not null)
        {
            request.Content = System.Net.Http.Json.JsonContent.Create(body);
        }

        if (XsrfToken is { } token)
        {
            request.Headers.Add("X-XSRF-TOKEN", token);
        }

        return Http.SendAsync(request, TestContext.Current.CancellationToken);
    }

    /// <summary>Primes antiforgery and signs in.</summary>
    public async Task<HttpResponseMessage> SignInAsync(string badge = "OP-1001", string? shift = "A")
    {
        await PrimeAntiforgeryAsync();
        return await SendAsync(HttpMethod.Post, "/api/session", new { badgeId = badge, shift });
    }

    /// <summary>GET a relative URL.</summary>
    public Task<HttpResponseMessage> GetAsync(string url) =>
        Http.GetAsync(new Uri(url, UriKind.Relative), TestContext.Current.CancellationToken);

    /// <inheritdoc />
    public void Dispose() => Http.Dispose();
}
