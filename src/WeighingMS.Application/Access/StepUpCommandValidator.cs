using FluentValidation;

namespace WeighingMS.Application.Access;

/// <summary>Validates step-up input: a supervisor badge, a 4–8 digit PIN and a known action.</summary>
public sealed class StepUpCommandValidator : AbstractValidator<StepUpCommand>
{
    /// <summary>Creates the validator with the configured actions.</summary>
    public StepUpCommandValidator(StationAccessOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        RuleFor(c => c.SupervisorBadgeId)
            .Must(b => BadgeFormat.IsValid(b))
            .WithMessage("Badge must be 1 to 32 letters, digits or dashes.")
            .WithErrorCode("Request.Invalid");
        RuleFor(c => c.Pin).NotEmpty().Matches("^[0-9]{4,8}$").WithMessage("PIN must be 4 to 8 digits.").WithErrorCode("Request.Invalid");
        RuleFor(c => c.Action).Must(a => options.StepUpActions.Contains(a)).WithMessage("Unknown action.").WithErrorCode("Request.Invalid");
    }
}
