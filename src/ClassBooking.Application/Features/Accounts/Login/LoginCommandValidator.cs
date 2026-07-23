using FluentValidation;

namespace ClassBooking.Application.Features.Accounts.Login;

// Presence and ceiling only, on purpose: judging the e-mail format or a minimum length would turn a
// nonexistent or legacy credential into a 400, leaking what the uniform 401 hides. Password is not trimmed.
internal sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
  public LoginCommandValidator()
  {
    RuleFor(command => (command.Email ?? string.Empty).Trim())
        .Cascade(CascadeMode.Stop)
        .NotEmpty()
        .MaximumLength(254)
        .OverridePropertyName("Email");

    RuleFor(command => command.Password)
        .Cascade(CascadeMode.Stop)
        .NotEmpty()
        .MaximumLength(100);
  }
}
