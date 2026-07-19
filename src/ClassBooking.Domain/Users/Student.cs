namespace ClassBooking.Domain.Users;

public sealed class Student : User
{
  private Student(Guid id, string name, string email, string passwordHash, DateTimeOffset createdAt)
      : base(id, name, email, passwordHash, UserRole.Student, createdAt)
  {
  }

  public static Student Register(string name, string email, string passwordHash, DateTimeOffset createdAt)
  {
    Student student = new Student(Guid.CreateVersion7(createdAt), name, email, passwordHash, createdAt);
    student.Raise(new StudentRegisteredDomainEvent(student.Id, createdAt));

    return student;
  }
}
