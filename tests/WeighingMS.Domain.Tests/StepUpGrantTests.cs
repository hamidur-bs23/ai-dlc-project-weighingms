using WeighingMS.Domain.Access;

namespace WeighingMS.Domain.Tests;

public sealed class StepUpGrantTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 3, 21, 48, 22, TimeSpan.Zero);
    private static readonly StationUser _supervisor = StationUser.CreateSupervisor("SV-2001", "M. Rahman", "hash");

    private static StepUpGrant Grant() => StepUpGrant.Issue(_supervisor, "override", _now, TimeSpan.FromMinutes(2));

    [Fact]
    public void Issue_CarriesSupervisorActionAndExpiry()
    {
        var grant = Grant();

        grant.SupervisorBadgeId.ShouldBe("SV-2001");
        grant.SupervisorName.ShouldBe("M. Rahman");
        grant.Action.ShouldBe("override");
        grant.ExpiresAt.ShouldBe(_now.AddMinutes(2));
        grant.UsedAt.ShouldBeNull();
    }

    [Fact]
    public void Issue_ToAnOperator_Throws()
    {
        Should.Throw<InvalidOperationException>(() =>
            StepUpGrant.Issue(StationUser.CreateOperator("OP-1", "X"), "override", _now, TimeSpan.FromMinutes(2)));
    }

    [Fact]
    public void Consume_ValidGrant_SucceedsOnceThenFails()
    {
        var grant = Grant();

        grant.Consume("override", _now.AddSeconds(30)).IsError.ShouldBeFalse();
        grant.UsedAt.ShouldBe(_now.AddSeconds(30));

        var second = grant.Consume("override", _now.AddSeconds(31));
        second.IsError.ShouldBeTrue();
        second.FirstError.Code.ShouldBe("Auth.GrantInvalid");
        second.FirstError.Description.ShouldContain("already used");
    }

    [Theory]
    [InlineData(120)]
    [InlineData(121)]
    public void Consume_AtOrAfterExpiry_Fails(int secondsLater)
    {
        var result = Grant().Consume("override", _now.AddSeconds(secondsLater));

        result.IsError.ShouldBeTrue();
        result.FirstError.Description.ShouldContain("expired");
    }

    [Fact]
    public void Consume_ForAnotherAction_FailsAndStaysUnused()
    {
        var grant = Grant();

        var result = grant.Consume("reprint", _now);

        result.IsError.ShouldBeTrue();
        result.FirstError.Description.ShouldContain("override");
        grant.UsedAt.ShouldBeNull();
    }
}
