using ClassBooking.Application.Features.Accounts.Login;
using FluentAssertions;
using FluentValidation.Results;

namespace ClassBooking.Application.UnitTests.Features.Accounts.Login;

public sealed class LoginCommandValidatorTests
{
  private const string ValidEmail = "ana.souza@example.com";
  private const string ValidPassword = "s3nh4-segura";

  private readonly LoginCommandValidator _validator = new LoginCommandValidator();

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void should_reject_the_email_when_it_is_missing(string? email)
  {
    ValidationResult result = _validator.Validate(Command(email: email));

    result.Errors.Should().ContainSingle(failure => failure.PropertyName == "Email");
  }

  [Fact]
  public void should_reject_the_email_when_it_exceeds_the_column_width()
  {
    ValidationResult result = _validator.Validate(Command(email: Address(255)));

    result.Errors.Should().ContainSingle(failure => failure.PropertyName == "Email");
  }

  [Theory]
  [InlineData("ana")]
  [InlineData("ana@")]
  [InlineData("@example.com")]
  [InlineData("  ana.souza@example.com  ")]
  public void should_not_judge_the_email_format(string email)
  {
    // A malformed e-mail never exists normalized in the database, so it must fall into the same
    // uniform 401 as any other wrong credential instead of leaking structure through a 400.
    ValidationResult result = _validator.Validate(Command(email: email));

    result.Errors.Should().NotContain(failure => failure.PropertyName == "Email");
  }

  [Fact]
  public void should_accept_the_email_at_the_maximum_length()
  {
    ValidationResult result = _validator.Validate(Command(email: Address(254)));

    result.Errors.Should().NotContain(failure => failure.PropertyName == "Email");
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("       ")]
  public void should_reject_the_password_when_it_is_missing_or_blank(string? password)
  {
    // The registration rule rejects blank passwords the same way, so no legitimate credential
    // is ever made of spaces only and none turns into a 400 here.
    ValidationResult result = _validator.Validate(Command(password: password));

    result.Errors.Should().ContainSingle(failure => failure.PropertyName == "Password");
  }

  [Fact]
  public void should_reject_the_password_when_it_exceeds_the_registration_ceiling()
  {
    ValidationResult result = _validator.Validate(Command(password: new string('a', 101)));

    result.Errors.Should().ContainSingle(failure => failure.PropertyName == "Password");
  }

  [Fact]
  public void should_not_require_the_registration_minimum_for_the_password()
  {
    // An account created under an older, shorter policy must reach the uniform 401, never a 400.
    ValidationResult result = _validator.Validate(Command(password: new string('a', 7)));

    result.Errors.Should().NotContain(failure => failure.PropertyName == "Password");
  }

  [Fact]
  public void should_measure_the_password_without_trimming()
  {
    ValidationResult result = _validator.Validate(Command(password: " " + new string('a', 99) + " "));

    result.Errors.Should().ContainSingle(failure => failure.PropertyName == "Password");
  }

  [Fact]
  public void should_accept_the_password_at_the_ceiling()
  {
    ValidationResult result = _validator.Validate(Command(password: new string('a', 100)));

    result.Errors.Should().NotContain(failure => failure.PropertyName == "Password");
  }

  private static string Address(int length)
  {
    const string Domain = "@example.com";

    return new string('a', length - Domain.Length) + Domain;
  }

  private static LoginCommand Command(string? email = ValidEmail, string? password = ValidPassword) =>
      new LoginCommand(email!, password!);
}
