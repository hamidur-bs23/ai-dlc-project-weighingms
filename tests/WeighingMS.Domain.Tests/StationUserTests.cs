using WeighingMS.Domain.Access;

namespace WeighingMS.Domain.Tests;

public sealed class StationUserTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 3, 21, 48, 22, TimeSpan.Zero);
    private static readonly LockoutPolicy _policy = new(5, TimeSpan.FromMinutes(5));

    private static StationUser Supervisor() => StationUser.CreateSupervisor(" sv-2001 ", "M. Rahman", "hash");

    [Fact]
    public void Create_TrimsAndUppercasesTheBadge()
    {
        StationUser.CreateOperator(" op-1001 ", " R. Karim ").BadgeId.ShouldBe("OP-1001");
        Supervisor().BadgeId.ShouldBe("SV-2001");
    }

    [Fact]
    public void CreateOperator_HasNoPinAndOperatorRole()
    {
        var user = StationUser.CreateOperator("OP-1001", "R. Karim");

        user.Role.ShouldBe(StationRole.Operator);
        user.PinHash.ShouldBeNull();
        user.Id.Version.ShouldBe(7);
    }

    [Fact]
    public void CreateSupervisor_WithoutPinHash_Throws()
    {
        Should.Throw<ArgumentException>(() => StationUser.CreateSupervisor("SV-1", "X", " "));
    }

    [Fact]
    public void RegisterFailedPin_BelowLimit_DoesNotLock()
    {
        var sv = Supervisor();
        for (var i = 0; i < 4; i++)
        {
            sv.RegisterFailedPin(_now, _policy).ShouldBeFalse();
        }

        sv.IsLockedOut(_now).ShouldBeFalse();
        sv.FailedPinAttempts.ShouldBe(4);
    }

    [Fact]
    public void RegisterFailedPin_FifthFailure_LocksForTheLockoutDuration()
    {
        var sv = Supervisor();
        for (var i = 0; i < 4; i++)
        {
            sv.RegisterFailedPin(_now, _policy);
        }

        sv.RegisterFailedPin(_now, _policy).ShouldBeTrue();

        sv.IsLockedOut(_now.AddMinutes(4).AddSeconds(59)).ShouldBeTrue();
        sv.IsLockedOut(_now.AddMinutes(5)).ShouldBeFalse();
        sv.FailedPinAttempts.ShouldBe(0);
    }

    [Fact]
    public void RegisterSuccessfulPin_ClearsFailuresAndLock()
    {
        var sv = Supervisor();
        sv.RegisterFailedPin(_now, _policy);
        sv.RegisterSuccessfulPin();

        sv.FailedPinAttempts.ShouldBe(0);
        sv.LockedUntil.ShouldBeNull();
    }
}
