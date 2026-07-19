using ClassBooking.Application.Abstractions.Messaging;
using ClassBooking.Application.Behaviors;
using ClassBooking.Application.Common;
using ClassBooking.Domain.Common;
using FluentAssertions;
using FluentValidation;

namespace ClassBooking.Application.UnitTests.Behaviors;

public sealed class ValidationBehaviorTests
{
  private sealed record RegisterCommand(string Email, string FullName) : ICommand<Guid>;

  private sealed class EmailValidator : AbstractValidator<RegisterCommand>
  {
    public EmailValidator() => RuleFor(command => command.Email).NotEmpty().WithMessage("Email is required.");
  }

  private sealed class FullNameValidator : AbstractValidator<RegisterCommand>
  {
    public FullNameValidator() => RuleFor(command => command.FullName).NotEmpty().WithMessage("Full name is required.");
  }

  private static readonly Guid UserId = Guid.CreateVersion7();

  private static Task<Result<Guid>> Handler(CancellationToken cancellationToken = default) => Task.FromResult(Result.Success(UserId));

  [Fact]
  public async Task should_invoke_handler_when_no_validators_registered()
  {
    ValidationBehavior<RegisterCommand, Result<Guid>> behavior = new ValidationBehavior<RegisterCommand, Result<Guid>>([]);

    Result<Guid> response = await behavior.Handle(new RegisterCommand("ana@example.com", "Ana"), Handler, CancellationToken.None);

    response.IsSuccess.Should().BeTrue();
    response.Value.Should().Be(UserId);
  }

  [Fact]
  public async Task should_invoke_handler_when_request_is_valid()
  {
    ValidationBehavior<RegisterCommand, Result<Guid>> behavior = new ValidationBehavior<RegisterCommand, Result<Guid>>([new EmailValidator(), new FullNameValidator()]);

    Result<Guid> response = await behavior.Handle(new RegisterCommand("ana@example.com", "Ana"), Handler, CancellationToken.None);

    response.IsSuccess.Should().BeTrue();
  }

  [Fact]
  public async Task should_short_circuit_with_validation_error_when_request_is_invalid()
  {
    bool handlerInvoked = false;
    ValidationBehavior<RegisterCommand, Result<Guid>> behavior = new ValidationBehavior<RegisterCommand, Result<Guid>>([new EmailValidator()]);

    Result<Guid> response = await behavior.Handle(
        new RegisterCommand(string.Empty, "Ana"),
        _ =>
        {
          handlerInvoked = true;
          return Handler();
        },
        CancellationToken.None);

    handlerInvoked.Should().BeFalse();
    response.IsFailure.Should().BeTrue();
    response.Error.Code.Should().Be("ValidationFailed");
  }

  [Fact]
  public async Task should_group_messages_by_camel_case_field_when_multiple_validators_fail()
  {
    ValidationBehavior<RegisterCommand, Result<Guid>> behavior = new ValidationBehavior<RegisterCommand, Result<Guid>>([new EmailValidator(), new FullNameValidator()]);

    Result<Guid> response = await behavior.Handle(new RegisterCommand(string.Empty, string.Empty), Handler, CancellationToken.None);

    ValidationError validationError = response.Error.Should().BeOfType<ValidationError>().Subject;
    validationError.Errors.Should().ContainKey("email").WhoseValue.Should().Equal("Email is required.");
    validationError.Errors.Should().ContainKey("fullName").WhoseValue.Should().Equal("Full name is required.");
  }

  [Fact]
  public async Task should_fail_with_validation_error_when_response_is_non_generic_result()
  {
    DeactivateCommand deactivate = new DeactivateCommand(string.Empty);
    ValidationBehavior<DeactivateCommand, Result> behavior = new ValidationBehavior<DeactivateCommand, Result>([new DeactivateValidator()]);

    Result response = await behavior.Handle(deactivate, _ => Task.FromResult(Result.Success()), CancellationToken.None);

    response.IsFailure.Should().BeTrue();
    response.Error.Should().BeOfType<ValidationError>();
  }

  private sealed record DeactivateCommand(string Reason) : ICommand;

  private sealed class DeactivateValidator : AbstractValidator<DeactivateCommand>
  {
    public DeactivateValidator() => RuleFor(command => command.Reason).NotEmpty();
  }
}
