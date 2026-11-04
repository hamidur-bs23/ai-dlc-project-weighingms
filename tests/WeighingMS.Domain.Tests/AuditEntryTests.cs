using WeighingMS.Domain.Audit;

namespace WeighingMS.Domain.Tests;

public sealed class AuditEntryTests
{
    [Fact]
    public void Record_StoresTheEventInUtc()
    {
        var local = new DateTimeOffset(2026, 11, 4, 3, 48, 22, TimeSpan.FromHours(6));

        var entry = AuditEntry.Record(AuditEventType.StepUpGranted, "OP-1001", local, subject: "SV-2001", reason: "override");

        entry.OccurredAt.ShouldBe(new DateTimeOffset(2026, 11, 3, 21, 48, 22, TimeSpan.Zero));
        entry.OccurredAt.Offset.ShouldBe(TimeSpan.Zero);
        entry.Type.ShouldBe(AuditEventType.StepUpGranted);
        entry.Subject.ShouldBe("SV-2001");
        entry.Reason.ShouldBe("override");
    }

    [Fact]
    public void Record_WithoutActor_Throws()
    {
        Should.Throw<ArgumentException>(() => AuditEntry.Record(AuditEventType.SignIn, " ", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void AuditEntry_HasNoPublicSetters()
    {
        typeof(AuditEntry).GetProperties()
            .Where(p => p.SetMethod is { IsPublic: true })
            .ShouldBeEmpty();
    }
}
