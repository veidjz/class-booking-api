using FluentValidation;

namespace ClassBooking.Application.Features.Accounts.Login;

/// <remarks>
/// Presence and ceiling only: judging the e-mail format or the registration minimum here would
/// turn a nonexistent or legacy credential into a 400, leaking the structure that the uniform
/// 401 exists to hide. The password is measured exactly as sent, never trimmed.
/// </remarks>
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
