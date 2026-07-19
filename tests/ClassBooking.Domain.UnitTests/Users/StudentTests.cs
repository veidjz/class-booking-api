using ClassBooking.Domain.Users;
using FluentAssertions;

namespace ClassBooking.Domain.UnitTests.Users;

public sealed class StudentTests
{
  private static readonly DateTimeOffset CreatedAt = new DateTimeOffset(2026, 3, 2, 12, 0, 0, TimeSpan.Zero);

  [Fact]
  public void should_create_an_active_student_account_when_registered()
  {
    Student student = Student.Register("Ana Souza", "ana.souza@example.com", "hash", CreatedAt);

    student.Name.Should().Be("Ana Souza");
    student.Email.Should().Be("ana.souza@example.com");
    student.PasswordHash.Should().Be("hash");
    student.Role.Should().Be(UserRole.Student);
    student.IsActive.Should().BeTrue();
    student.CreatedAt.Should().Be(CreatedAt);
  }

  [Fact]
  public void should_identify_the_student_with_a_time_ordered_uuid_when_registered()
  {
    Student first = Student.Register("Ana Souza", "ana.souza@example.com", "hash", CreatedAt);
    Student second = Student.Register("Bruno Dias", "bruno.dias@example.com", "hash", CreatedAt);

    first.Id.Version.Should().Be(7);
    second.Id.Should().NotBe(first.Id);
  }

  [Fact]
  public void should_raise_a_student_registered_event_when_registered()
  {
    Student student = Student.Register("Ana Souza", "ana.souza@example.com", "hash", CreatedAt);

    student.DomainEvents.Should().ContainSingle()
        .Which.Should().Be(new StudentRegisteredDomainEvent(student.Id, CreatedAt));
  }
}
