using FluentValidation;

namespace WeighingMS.Application.Access;

/// <summary>Validates sign-in input.</summary>
public sealed class SignInCommandValidator : AbstractValidator<SignInCommand>
{
    /// <summary>Badge: 1–32 letters, digits or dashes. Shift: up to 16 characters.</summary>
    public SignInCommandValidator()
    {
        RuleFor(c => c.BadgeId).NotEmpty().MaximumLength(32).Matches("^[A-Za-z0-9-]+$").WithErrorCode("Request.Invalid");
        RuleFor(c => c.Shift).MaximumLength(16).WithErrorCode("Request.Invalid");
    }
}
