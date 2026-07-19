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
  public void should_derive_the_identity_from_the_creation_instant_when_registered()
  {
    Student student = Student.Register("Ana Souza", "ana.souza@example.com", "hash", CreatedAt);

    student.Id.Version.Should().Be(7);
    Timestamp(student.Id).Should().Be(CreatedAt.ToUnixTimeMilliseconds());
  }

  [Fact]
  public void should_identify_students_with_time_ordered_uuids_when_registered_in_sequence()
  {
    Student first = Student.Register("Ana Souza", "ana.souza@example.com", "hash", CreatedAt);
    Student second = Student.Register("Bruno Dias", "bruno.dias@example.com", "hash", CreatedAt.AddSeconds(1));

    second.Id.Should().NotBe(first.Id);
    first.Id.ToByteArray(bigEndian: true).AsSpan()
        .SequenceCompareTo(second.Id.ToByteArray(bigEndian: true))
        .Should().BeNegative();
  }

  private static long Timestamp(Guid id)
  {
    byte[] bytes = id.ToByteArray(bigEndian: true);

    return ((long)bytes[0] << 40)
        | ((long)bytes[1] << 32)
        | ((long)bytes[2] << 24)
        | ((long)bytes[3] << 16)
        | ((long)bytes[4] << 8)
        | bytes[5];
  }

  [Fact]
  public void should_raise_a_student_registered_event_when_registered()
  {
    Student student = Student.Register("Ana Souza", "ana.souza@example.com", "hash", CreatedAt);

    student.DomainEvents.Should().ContainSingle()
        .Which.Should().Be(new StudentRegisteredDomainEvent(student.Id, CreatedAt));
  }
}
