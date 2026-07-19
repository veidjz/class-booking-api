using ClassBooking.Domain.Users;
using FluentAssertions;

namespace ClassBooking.Domain.UnitTests.Users;

public sealed class TeacherTests
{
  private static readonly DateTimeOffset CreatedAt = new DateTimeOffset(2026, 3, 2, 12, 0, 0, TimeSpan.Zero);

  [Fact]
  public void should_create_an_active_teacher_account_when_created()
  {
    Teacher teacher = Teacher.Create("Carlos Lima", "carlos.lima@example.com", "hash", CreatedAt);

    teacher.Name.Should().Be("Carlos Lima");
    teacher.Email.Should().Be("carlos.lima@example.com");
    teacher.PasswordHash.Should().Be("hash");
    teacher.Role.Should().Be(UserRole.Teacher);
    teacher.IsActive.Should().BeTrue();
    teacher.CreatedAt.Should().Be(CreatedAt);
  }

  [Fact]
  public void should_start_the_reliability_counters_at_zero_when_created()
  {
    Teacher teacher = Teacher.Create("Carlos Lima", "carlos.lima@example.com", "hash", CreatedAt);

    teacher.CancellationCount.Should().Be(0);
    teacher.LateCancellationCount.Should().Be(0);
    teacher.NoShowCount.Should().Be(0);
  }

  [Fact]
  public void should_raise_a_teacher_account_created_event_when_created()
  {
    Teacher teacher = Teacher.Create("Carlos Lima", "carlos.lima@example.com", "hash", CreatedAt);

    teacher.DomainEvents.Should().ContainSingle()
        .Which.Should().Be(new TeacherAccountCreatedDomainEvent(teacher.Id, CreatedAt));
  }
}
