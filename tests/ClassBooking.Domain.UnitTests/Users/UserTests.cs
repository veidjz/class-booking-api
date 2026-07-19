using System.Reflection;
using ClassBooking.Domain.Users;
using FluentAssertions;

namespace ClassBooking.Domain.UnitTests.Users;

public sealed class UserTests
{
  private static readonly DateTimeOffset CreatedAt = new DateTimeOffset(2026, 3, 2, 12, 0, 0, TimeSpan.Zero);

  [Fact]
  public void should_create_an_active_admin_account_when_created()
  {
    User admin = User.CreateAdmin("Root", "root@example.com", "hash", CreatedAt);

    admin.Name.Should().Be("Root");
    admin.Email.Should().Be("root@example.com");
    admin.PasswordHash.Should().Be("hash");
    admin.Role.Should().Be(UserRole.Admin);
    admin.IsActive.Should().BeTrue();
    admin.CreatedAt.Should().Be(CreatedAt);
  }

  [Fact]
  public void should_not_raise_events_when_an_admin_is_created()
  {
    User admin = User.CreateAdmin("Root", "root@example.com", "hash", CreatedAt);

    admin.DomainEvents.Should().BeEmpty();
  }

  [Fact]
  public void should_expose_no_write_path_for_the_role()
  {
    PropertyInfo role = typeof(User).GetProperty(nameof(User.Role))!;

    role.SetMethod.Should().BeNull();
    typeof(User).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
        .Where(field => field.FieldType == typeof(UserRole))
        .Should().OnlyContain(field => field.IsInitOnly);
  }
}
