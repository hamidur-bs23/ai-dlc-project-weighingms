using Microsoft.Extensions.Time.Testing;
using WeighingMS.Application.Access;
using WeighingMS.Domain.Access;
using WeighingMS.Domain.Audit;

namespace WeighingMS.Application.Tests;

public sealed class StationAccessServiceTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 3, 21, 48, 22, TimeSpan.Zero));
    private readonly FakeUsers _users = new();
    private readonly FakeGrants _grants = new();
    private readonly FakeAudit _audit = new();
    private readonly FakeUnitOfWork _uow = new();
    private readonly FakePinHasher _hasher = new();
    private readonly StationAccessOptions _options = new();
    private readonly StationAccessService _service;

    public StationAccessServiceTests()
    {
        _users.Users.Add(StationUser.CreateOperator("OP-1001", "R. Karim"));
        _users.Users.Add(StationUser.CreateSupervisor("SV-2001", "M. Rahman", _hasher.Hash("2468")));
        _service = new StationAccessService(
            _users, _grants, _hasher, _audit, _uow, new FakePlantClock(_time), _options,
            new SignInCommandValidator(), new StepUpCommandValidator(_options));
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private Task<ErrorOr.ErrorOr<StepUpGrantResult>> StepUpAsync(string badge, string pin, string action = "override") =>
        _service.RequestStepUpAsync(new StepUpCommand("OP-1001", badge, pin, action), Ct);

    [Fact]
    public async Task SignIn_KnownBadge_ReturnsSessionAndAudits()
    {
        var result = await _service.SignInAsync(new SignInCommand(" op-1001 ", "A", null), Ct);

        result.IsError.ShouldBeFalse();
        result.Value.DisplayName.ShouldBe("R. Karim");
        result.Value.Shift.ShouldBe("A");
        result.Value.StationId.ShouldBe("STATION 01");
        result.Value.TimeZone.ShouldBe("Asia/Dhaka");
        _audit.Entries.Single().Type.ShouldBe(AuditEventType.SignIn);
        _uow.Saves.ShouldBe(1);
    }

    [Fact]
    public async Task SignIn_UnknownBadge_ReturnsBadgeNotRecognisedAndWritesNothing()
    {
        var result = await _service.SignInAsync(new SignInCommand("OP-9999", null, null), Ct);

        result.FirstError.Code.ShouldBe("Auth.BadgeNotRecognised");
        _audit.Entries.ShouldBeEmpty();
        _uow.Saves.ShouldBe(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("OP 1001")]
    [InlineData("<script>")]
    public async Task SignIn_InvalidBadge_ReturnsValidationError(string badge)
    {
        var result = await _service.SignInAsync(new SignInCommand(badge, null, null), Ct);

        result.FirstError.Type.ShouldBe(ErrorOr.ErrorType.Validation);
        result.FirstError.Code.ShouldBe("Request.Invalid");
    }

    [Fact]
    public async Task SignIn_ReplacingASession_AuditsTheReplacement()
    {
        await _service.SignInAsync(new SignInCommand("SV-2001", null, "OP-1001"), Ct);

        _audit.Entries.Select(e => e.Type).ShouldBe([AuditEventType.SessionReplaced, AuditEventType.SignIn]);
        _audit.Entries[0].Subject.ShouldBe("OP-1001");
    }

    [Fact]
    public async Task SignOut_Audits()
    {
        await _service.SignOutAsync("OP-1001", Ct);

        _audit.Entries.Single().Type.ShouldBe(AuditEventType.SignOut);
    }

    [Fact]
    public async Task StepUp_CorrectPin_IssuesGrantAndAudits()
    {
        var result = await StepUpAsync("SV-2001", "2468");

        result.IsError.ShouldBeFalse();
        result.Value.SupervisorName.ShouldBe("M. Rahman");
        result.Value.ExpiresAt.ShouldBe(_time.GetUtcNow().AddMinutes(2));
        _grants.Grants.Single().Id.ShouldBe(result.Value.GrantId);
        _audit.Entries.Single().Type.ShouldBe(AuditEventType.StepUpGranted);
    }

    [Fact]
    public async Task StepUp_WrongPin_FailsAndAuditsWithoutGrant()
    {
        var result = await StepUpAsync("SV-2001", "0000");

        result.FirstError.Code.ShouldBe("Auth.StepUpFailed");
        _grants.Grants.ShouldBeEmpty();
        _audit.Entries.Single().Reason.ShouldBe("wrong PIN");
    }

    [Fact]
    public async Task StepUp_OperatorBadge_ReturnsSupervisorRequired()
    {
        var result = await StepUpAsync("OP-1001", "1234");

        result.FirstError.Code.ShouldBe("Auth.SupervisorRequired");
        _audit.Entries.Single().Reason.ShouldBe("not a supervisor");
    }

    [Fact]
    public async Task StepUp_UnknownBadge_ReturnsStepUpFailed()
    {
        (await StepUpAsync("SV-9999", "2468")).FirstError.Code.ShouldBe("Auth.StepUpFailed");
    }

    [Fact]
    public async Task StepUp_FiveWrongPins_LocksOutEvenForTheCorrectPin()
    {
        for (var i = 0; i < 4; i++)
        {
            (await StepUpAsync("SV-2001", "0000")).FirstError.Code.ShouldBe("Auth.StepUpFailed");
        }

        (await StepUpAsync("SV-2001", "0000")).FirstError.Code.ShouldBe("Auth.LockedOut");
        (await StepUpAsync("SV-2001", "2468")).FirstError.Code.ShouldBe("Auth.LockedOut");
        _audit.Entries.Count(e => e.Type == AuditEventType.StepUpLockedOut).ShouldBe(1);

        _time.Advance(TimeSpan.FromMinutes(5));
        (await StepUpAsync("SV-2001", "2468")).IsError.ShouldBeFalse();
    }

    [Theory]
    [InlineData("12", "override")]
    [InlineData("123456789", "override")]
    [InlineData("12ab", "override")]
    [InlineData("2468", "fly")]
    public async Task StepUp_InvalidPinOrAction_ReturnsValidationError(string pin, string action)
    {
        var result = await StepUpAsync("SV-2001", pin, action);

        result.FirstError.Type.ShouldBe(ErrorOr.ErrorType.Validation);
        _audit.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task ConsumeGrant_OnceForItsAction_ThenRejected()
    {
        var grantId = (await StepUpAsync("SV-2001", "2468")).Value.GrantId;

        (await _service.ConsumeGrantAsync(grantId, "override", Ct)).IsError.ShouldBeFalse();
        (await _service.ConsumeGrantAsync(grantId, "override", Ct)).FirstError.Code.ShouldBe("Auth.GrantInvalid");
    }

    [Fact]
    public async Task ConsumeGrant_UnknownOrExpired_Rejected()
    {
        (await _service.ConsumeGrantAsync(Guid.NewGuid(), "override", Ct)).FirstError.Code.ShouldBe("Auth.GrantInvalid");

        var grantId = (await StepUpAsync("SV-2001", "2468")).Value.GrantId;
        _time.Advance(TimeSpan.FromMinutes(2));
        (await _service.ConsumeGrantAsync(grantId, "override", Ct)).FirstError.Description.ShouldContain("expired");
    }
}
