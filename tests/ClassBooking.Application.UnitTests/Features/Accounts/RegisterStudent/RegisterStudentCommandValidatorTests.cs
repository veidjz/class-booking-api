using ClassBooking.Application.Features.Accounts.RegisterStudent;
using FluentAssertions;
using FluentValidation.Results;

namespace ClassBooking.Application.UnitTests.Features.Accounts.RegisterStudent;

public sealed class RegisterStudentCommandValidatorTests
{
  private const string ValidName = "Ana Souza";
  private const string ValidEmail = "ana.souza@example.com";
  private const string ValidPassword = "s3nh4-segura";

  private readonly RegisterStudentCommandValidator _validator = new RegisterStudentCommandValidator();

  public static TheoryData<string?> InvalidNames() =>
      new TheoryData<string?> { null, "", "   ", "A", " A ", new string('a', 121) };

  public static TheoryData<string> ValidNames() =>
      new TheoryData<string> { "An", new string('a', 120), "  Ana  " };

  [Theory]
  [MemberData(nameof(InvalidNames))]
  public void should_reject_the_name_when_it_is_missing_or_outside_the_trimmed_bounds(string? name)
  {
    ValidationResult result = _validator.Validate(Command(name: name));

    result.Errors.Should().ContainSingle(failure => failure.PropertyName == "Name");
  }

  [Theory]
  [MemberData(nameof(ValidNames))]
  public void should_accept_the_name_at_the_trimmed_bounds(string name)
  {
    ValidationResult result = _validator.Validate(Command(name: name));

    result.Errors.Should().NotContain(failure => failure.PropertyName == "Name");
  }

  private static RegisterStudentCommand Command(
      string? name = ValidName,
      string? email = ValidEmail,
      string? password = ValidPassword) =>
      new RegisterStudentCommand(name!, email!, password!);
}
