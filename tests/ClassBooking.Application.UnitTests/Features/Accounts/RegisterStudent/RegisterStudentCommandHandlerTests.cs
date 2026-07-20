using ClassBooking.Application.Abstractions.Auth;
using ClassBooking.Application.Features.Accounts.RegisterStudent;
using ClassBooking.Domain.Common;
using ClassBooking.Domain.Users;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace ClassBooking.Application.UnitTests.Features.Accounts.RegisterStudent;

public sealed class RegisterStudentCommandHandlerTests
{
  private static readonly DateTimeOffset Now = new DateTimeOffset(2026, 3, 2, 12, 0, 0, TimeSpan.Zero);
  private const string PasswordHash = "$2a$12$hashed";

  private readonly IUserRepository _users = Substitute.For<IUserRepository>();
  private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
  private readonly FakeTimeProvider _clock = new FakeTimeProvider(Now) { AutoAdvanceAmount = TimeSpan.FromSeconds(1) };
  private readonly RegisterStudentCommandHandler _handler;

  private User? _added;

  public RegisterStudentCommandHandlerTests()
  {
    _passwordHasher.Hash(Arg.Any<string>()).Returns(PasswordHash);
    _users.Add(Arg.Do<User>(user => _added = user));
    _handler = new RegisterStudentCommandHandler(_users, _passwordHasher, _clock);
  }

  [Fact]
  public async Task should_add_an_active_student_with_the_hashed_password()
  {
    await _handler.Handle(Command(), CancellationToken.None);

    _added.Should().NotBeNull();
    _added.Should().BeOfType<Student>();
    _added!.Role.Should().Be(UserRole.Student);
    _added.IsActive.Should().BeTrue();
    _added.PasswordHash.Should().Be(PasswordHash);
  }

  [Fact]
  public async Task should_hash_the_submitted_password_untouched()
  {
    await _handler.Handle(Command(password: "  s3nh4 com espaco  "), CancellationToken.None);

    _passwordHasher.Received(1).Hash("  s3nh4 com espaco  ");
  }

  [Fact]
  public async Task should_stamp_the_account_with_the_injected_instant()
  {
    await _handler.Handle(Command(), CancellationToken.None);

    _added!.CreatedAt.Should().Be(Now);
  }

  [Fact]
  public async Task should_return_the_created_account()
  {
    Result<RegisterStudentResponse> result = await _handler.Handle(Command(), CancellationToken.None);

    result.IsSuccess.Should().BeTrue();
    result.Value.Should().BeEquivalentTo(new RegisterStudentResponse(
        _added!.Id,
        "Ana Souza",
        "ana.souza@example.com",
        UserRole.Student,
        true,
        Now));
  }

  [Fact]
  public async Task should_persist_the_email_trimmed_and_lowercased()
  {
    await _handler.Handle(Command(email: "  ANA@Example.COM  "), CancellationToken.None);

    _added!.Email.Should().Be("ana@example.com");
  }

  [Fact]
  public async Task should_persist_the_name_trimmed()
  {
    await _handler.Handle(Command(name: "  Ana Souza  "), CancellationToken.None);

    _added!.Name.Should().Be("Ana Souza");
  }

  [Fact]
  public async Task should_return_the_normalized_values()
  {
    Result<RegisterStudentResponse> result =
        await _handler.Handle(Command("  Ana Souza  ", "  ANA@Example.COM  "), CancellationToken.None);

    result.Value.Name.Should().Be("Ana Souza");
    result.Value.Email.Should().Be("ana@example.com");
  }

  [Fact]
  public async Task should_return_email_already_in_use_when_the_email_belongs_to_an_account()
  {
    _users.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

    Result<RegisterStudentResponse> result = await _handler.Handle(Command(), CancellationToken.None);

    result.IsFailure.Should().BeTrue();
    result.Error.Should().Be(UserErrors.EmailAlreadyInUse);
    _users.DidNotReceive().Add(Arg.Any<User>());
    _passwordHasher.DidNotReceive().Hash(Arg.Any<string>());
  }

  [Fact]
  public async Task should_check_uniqueness_against_the_normalized_email()
  {
    await _handler.Handle(Command(email: "  ANA@Example.COM  "), CancellationToken.None);

    await _users.Received().ExistsByEmailAsync("ana@example.com", Arg.Any<CancellationToken>());
  }

  private static RegisterStudentCommand Command(
      string name = "Ana Souza",
      string email = "ana.souza@example.com",
      string password = "s3nh4-segura") =>
      new RegisterStudentCommand(name, email, password);
}
