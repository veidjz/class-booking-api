using FluentValidation;

namespace ClassBooking.Application.Features.Accounts.RegisterStudent;

internal sealed class RegisterStudentCommandValidator : AbstractValidator<RegisterStudentCommand>
{
  public RegisterStudentCommandValidator()
  {
    RuleFor(command => (command.Name ?? string.Empty).Trim())
        .Cascade(CascadeMode.Stop)
        .NotEmpty()
        .Length(2, 120)
        .OverridePropertyName("Name");

    RuleFor(command => (command.Email ?? string.Empty).Trim())
        .Cascade(CascadeMode.Stop)
        .NotEmpty()
        .EmailAddress()
        .MaximumLength(254)
        .OverridePropertyName("Email");
  }
}
