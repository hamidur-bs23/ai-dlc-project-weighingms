using FluentValidation;

namespace WeighingMS.Application.Access;

/// <summary>Validates sign-in input.</summary>
public sealed class SignInCommandValidator : AbstractValidator<SignInCommand>
{
    /// <summary>
    /// Badge: 1–32 letters, digits or dashes after trimming (a badge typed with surrounding spaces is accepted).
    /// Shift: up to 16 characters.
    /// </summary>
    public SignInCommandValidator()
    {
        RuleFor(c => c.BadgeId)
            .Must(b => BadgeFormat.IsValid(b))
            .WithMessage("Badge must be 1 to 32 letters, digits or dashes.")
            .WithErrorCode("Request.Invalid");
        RuleFor(c => c.Shift).MaximumLength(16).WithErrorCode("Request.Invalid");
    }
}
