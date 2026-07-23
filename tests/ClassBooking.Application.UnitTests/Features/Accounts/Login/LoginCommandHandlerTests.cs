using ClassBooking.Application.Abstractions.Auth;
using ClassBooking.Application.Features.Accounts.Login;
using ClassBooking.Domain.Common;
using ClassBooking.Domain.Users;
using FluentAssertions;
using NSubstitute;

namespace ClassBooking.Application.UnitTests.Features.Accounts.Login;

public sealed class LoginCommandHandlerTests
{
  private static readonly DateTimeOffset Now = new DateTimeOffset(2026, 3, 2, 12, 0, 0, TimeSpan.Zero);
  private const string Email = "ana.souza@example.com";
  private const string Password = "s3nh4-segura";
  private const string StoredHash = "$2a$12$stored";

  private readonly IUserRepository _users = Substitute.For<IUserRepository>();
  private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
  private readonly LoginCommandHandler _handler;

  public LoginCommandHandlerTests() =>
      _handler = new LoginCommandHandler(_users, _passwordHasher);

  [Fact]
  public async Task should_return_invalid_credentials_for_an_unknown_email()
  {
    Result<LoginResponse> result = await _handler.Handle(Command(), CancellationToken.None);

    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be("InvalidCredentials");
  }

  [Fact]
  public async Task should_burn_a_real_verification_for_an_unknown_email()
  {
    // Skipping the BCrypt cost would answer unknown e-mails in microseconds and known ones in
    // hundreds of milliseconds, telling the caller which e-mails exist.
    await _handler.Handle(Command(), CancellationToken.None);

    _passwordHasher.Received(1).Verify(Password, LoginCommandHandler.GhostPasswordHash);
  }

  [Fact]
  public void should_shape_the_ghost_hash_like_a_stored_one()
  {
    LoginCommandHandler.GhostPasswordHash.Should().StartWith("$2a$12$").And.HaveLength(60);
  }

  [Fact]
  public async Task should_return_invalid_credentials_for_a_wrong_password()
  {
    ActiveAccount();
    _passwordHasher.Verify(Password, StoredHash).Returns(false);

    Result<LoginResponse> result = await _handler.Handle(Command(), CancellationToken.None);

    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be("InvalidCredentials");
  }

  [Fact]
  public async Task should_return_invalid_credentials_for_a_deactivated_account_with_the_right_password()
  {
    DeactivatedAccount();
    _passwordHasher.Verify(Password, StoredHash).Returns(true);

    Result<LoginResponse> result = await _handler.Handle(Command(), CancellationToken.None);

    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be("InvalidCredentials");
  }

  [Fact]
  public async Task should_still_verify_the_password_of_a_deactivated_account()
  {
    // Short-circuiting on IsActive before the BCrypt cost would make deactivated accounts
    // distinguishable by timing; the verification always runs first.
    DeactivatedAccount();
    _passwordHasher.Verify(Password, StoredHash).Returns(true);

    await _handler.Handle(Command(), CancellationToken.None);

    _passwordHasher.Received(1).Verify(Password, StoredHash);
  }

  [Fact]
  public async Task should_look_up_the_account_by_the_normalized_email()
  {
    await _handler.Handle(Command(email: "  ANA.Souza@Example.COM  "), CancellationToken.None);

    await _users.Received(1).GetByEmailAsync(Email, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task should_not_hash_anything_on_any_failure()
  {
    DeactivatedAccount();
    _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

    await _handler.Handle(Command(), CancellationToken.None);
    await _handler.Handle(Command(email: "unknown@example.com"), CancellationToken.None);

    _passwordHasher.DidNotReceive().Hash(Arg.Any<string>());
  }

  private void ActiveAccount() => Account();

  private void DeactivatedAccount()
  {
    Student student = Account();
    student.Deactivate(Now);
  }

  private Student Account()
  {
    Student student = Student.Register("Ana Souza", Email, StoredHash, Now);
    _users.GetByEmailAsync(Email, Arg.Any<CancellationToken>()).Returns(student);

    return student;
  }

  private static LoginCommand Command(string email = Email, string password = Password) =>
      new LoginCommand(email, password);
}
