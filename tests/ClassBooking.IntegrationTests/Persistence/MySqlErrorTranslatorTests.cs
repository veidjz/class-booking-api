using ClassBooking.Domain.Common;
using ClassBooking.Domain.Users;
using ClassBooking.Infrastructure.Persistence;
using FluentAssertions;

namespace ClassBooking.IntegrationTests.Persistence;

public sealed class MySqlErrorTranslatorTests
{
  [Theory]
  [InlineData("Duplicate entry 'ana@classbooking.dev' for key 'users.ux_users_email'")]
  [InlineData("Duplicate entry 'ana@classbooking.dev' for key 'ux_users_email'")]
  [InlineData("Duplicate entry 'ana@classbooking.dev' for key 'classbooking.users.UX_USERS_EMAIL'")]
  public void should_translate_the_email_unique_index(string message)
  {
    Error? error = MySqlErrorTranslator.TranslateDuplicateKey(message);

    error.Should().Be(UserErrors.EmailAlreadyInUse);
  }

  [Theory]
  [InlineData("Duplicate entry '1' for key 'users.PRIMARY'")]
  [InlineData("Duplicate entry '1' for key 'teachers.ux_something_else'")]
  [InlineData("Cannot add or update a child row: a foreign key constraint fails")]
  [InlineData("Duplicate entry '1' for key 'unterminated")]
  public void should_not_translate_an_unknown_violation(string message)
  {
    Error? error = MySqlErrorTranslator.TranslateDuplicateKey(message);

    error.Should().BeNull();
  }
}
