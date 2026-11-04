using Microsoft.Extensions.Time.Testing;
using WeighingMS.Application.Abstractions;
using WeighingMS.Application.Access;
using WeighingMS.Domain.Access;
using WeighingMS.Domain.Audit;

namespace WeighingMS.Application.Tests;

/// <summary>Hand-written fakes at the port boundary (coding-standards: fakes over mocking libraries).</summary>
internal sealed class FakeUsers : IStationUserRepository
{
    public List<StationUser> Users { get; } = [];

    public Task<StationUser?> FindByBadgeAsync(string badgeId, CancellationToken cancellationToken) =>
        Task.FromResult(Users.FirstOrDefault(u => u.BadgeId == badgeId));
}

internal sealed class FakeGrants : IStepUpGrantStore
{
    public List<StepUpGrant> Grants { get; } = [];

    public void Add(StepUpGrant grant) => Grants.Add(grant);

    public Task<StepUpGrant?> FindAsync(Guid grantId, CancellationToken cancellationToken) =>
        Task.FromResult(Grants.FirstOrDefault(g => g.Id == grantId));
}

internal sealed class FakeAudit : IAuditTrail
{
    public List<AuditEntry> Entries { get; } = [];

    public void Append(AuditEntry entry) => Entries.Add(entry);
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int Saves { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        Saves++;
        return Task.CompletedTask;
    }
}

/// <summary>PIN "hash" = "hash:" + PIN, so tests can create supervisors without real hashing.</summary>
internal sealed class FakePinHasher : IPinHasher
{
    public string Hash(string pin) => "hash:" + pin;

    public bool Verify(StationUser user, string pin) => user.PinHash == Hash(pin);
}

internal sealed class FakePlantClock(FakeTimeProvider time) : IPlantClock
{
    public string TimeZoneId => "Asia/Dhaka";

    public DateTimeOffset UtcNow => time.GetUtcNow();

    public DateTimeOffset ToPlantTime(DateTimeOffset instant) => instant.ToOffset(TimeSpan.FromHours(6));
}
