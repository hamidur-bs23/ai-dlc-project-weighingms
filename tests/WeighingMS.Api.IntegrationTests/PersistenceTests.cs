using System.Net;
using Microsoft.EntityFrameworkCore;
using WeighingMS.Domain.Audit;

namespace WeighingMS.Api.IntegrationTests;

public sealed class PersistenceTests(ApiFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync() => await fixture.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Migrations_AreAllApplied()
    {
        await using var db = fixture.NewDbContext();

        (await db.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken)).ShouldBeEmpty();
        var applied = (await db.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)).ToList();
        applied.ShouldContain(m => m.EndsWith("_InitialStationAccess", StringComparison.Ordinal));
        applied.ShouldContain(m => m.EndsWith("_AddRecipeSnapshots", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AuditEntries_SurviveAnApiRestart()
    {
        await using (var api = fixture.CreateApi())
        {
            using var client = new StationClient(api);
            (await client.SignInAsync()).EnsureSuccessStatusCode();
        }

        await using var restarted = fixture.CreateApi();
        await using var db = fixture.NewDbContext();
        (await db.AuditEntries.CountAsync(e => e.Type == AuditEventType.SignIn, TestContext.Current.CancellationToken)).ShouldBe(1);
    }

    [Fact]
    public async Task AuditEntries_CannotBeModifiedOrDeleted()
    {
        await using (var api = fixture.CreateApi())
        {
            using var client = new StationClient(api);
            (await client.SignInAsync()).EnsureSuccessStatusCode();
        }

        await using var db = fixture.NewDbContext();
        var entry = await db.AuditEntries.FirstAsync(TestContext.Current.CancellationToken);
        db.AuditEntries.Remove(entry);

        await Should.ThrowAsync<InvalidOperationException>(() => db.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Health_Ready_ChecksTheDatabase()
    {
        await using var api = fixture.CreateApi();
        using var client = new StationClient(api);

        (await client.GetAsync("/health/ready")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync("/health/live")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Seeding_IsIdempotent()
    {
        await using var api = fixture.CreateApi();
        await using var db = fixture.NewDbContext();

        (await db.StationUsers.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(3);
    }
}
