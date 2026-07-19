using ClassBooking.Domain.Users;
using FluentAssertions;

namespace ClassBooking.Domain.UnitTests.Users;

public sealed class UserActivationTests
{
  private static readonly DateTimeOffset CreatedAt = new DateTimeOffset(2026, 3, 2, 12, 0, 0, TimeSpan.Zero);

  private static readonly DateTimeOffset ChangedAt = new DateTimeOffset(2026, 3, 3, 9, 30, 0, TimeSpan.Zero);

  private static Student RegisteredStudent()
  {
    Student student = Student.Register("Ana Souza", "ana.souza@example.com", "hash", CreatedAt);
    student.ClearDomainEvents();

    return student;
  }

  [Fact]
  public void should_deactivate_and_raise_an_event_when_the_account_is_active()
  {
    Student student = RegisteredStudent();

    bool changed = student.Deactivate(ChangedAt);

    changed.Should().BeTrue();
    student.IsActive.Should().BeFalse();
    student.DomainEvents.Should().ContainSingle()
        .Which.Should().Be(new AccountDeactivatedDomainEvent(student.Id, UserRole.Student, ChangedAt));
  }

  [Fact]
  public void should_not_raise_an_event_when_deactivating_an_inactive_account()
  {
    Student student = RegisteredStudent();
    student.Deactivate(ChangedAt);
    student.ClearDomainEvents();

    bool changed = student.Deactivate(ChangedAt);

    changed.Should().BeFalse();
    student.IsActive.Should().BeFalse();
    student.DomainEvents.Should().BeEmpty();
  }

  [Fact]
  public void should_activate_and_raise_an_event_when_the_account_is_inactive()
  {
    Student student = RegisteredStudent();
    student.Deactivate(ChangedAt);
    student.ClearDomainEvents();

    bool changed = student.Activate(ChangedAt);

    changed.Should().BeTrue();
    student.IsActive.Should().BeTrue();
    student.DomainEvents.Should().ContainSingle()
        .Which.Should().Be(new AccountActivatedDomainEvent(student.Id, UserRole.Student, ChangedAt));
  }

  [Fact]
  public void should_not_raise_an_event_when_activating_an_active_account()
  {
    Student student = RegisteredStudent();

    bool changed = student.Activate(ChangedAt);

    changed.Should().BeFalse();
    student.IsActive.Should().BeTrue();
    student.DomainEvents.Should().BeEmpty();
  }

  [Fact]
  public void should_carry_the_account_role_in_the_event_when_a_teacher_is_deactivated()
  {
    Teacher teacher = Teacher.Create("Carlos Lima", "carlos.lima@example.com", "hash", CreatedAt);
    teacher.ClearDomainEvents();

    teacher.Deactivate(ChangedAt);

    teacher.DomainEvents.Should().ContainSingle()
        .Which.Should().Be(new AccountDeactivatedDomainEvent(teacher.Id, UserRole.Teacher, ChangedAt));
  }
}
