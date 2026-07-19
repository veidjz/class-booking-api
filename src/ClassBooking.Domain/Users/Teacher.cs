namespace ClassBooking.Domain.Users;

public sealed class Teacher : User
{
  private Teacher(Guid id, string name, string email, string passwordHash, DateTimeOffset createdAt)
      : base(id, name, email, passwordHash, UserRole.Teacher, createdAt)
  {
  }

  public int CancellationCount { get; private set; }

  public int LateCancellationCount { get; private set; }

  public int NoShowCount { get; private set; }

  public static Teacher Create(string name, string email, string passwordHash, DateTimeOffset createdAt)
  {
    Teacher teacher = new Teacher(Guid.CreateVersion7(createdAt), name, email, passwordHash, createdAt);
    teacher.Raise(new TeacherAccountCreatedDomainEvent(teacher.Id, createdAt));

    return teacher;
  }
}
