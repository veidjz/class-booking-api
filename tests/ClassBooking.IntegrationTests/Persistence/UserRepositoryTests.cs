using ClassBooking.Application.Abstractions.Data;
using ClassBooking.Domain.Users;
using ClassBooking.IntegrationTests.Persistence.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace ClassBooking.IntegrationTests.Persistence;

[Collection(nameof(DatabaseCollection))]
public sealed class UserRepositoryTests(ContainersFixture fixture) : DatabaseTestBase(fixture)
{
  private static readonly DateTimeOffset CreatedAt = new DateTimeOffset(2026, 3, 2, 12, 0, 0, TimeSpan.Zero);

  [Fact]
  public async Task should_persist_the_student_in_the_base_and_subtype_tables()
  {
    await AddAsync(Student.Register("Ana", "ana@classbooking.dev", "hash", CreatedAt));

    await AssertRowCountsAsync(users: 1, students: 1, teachers: 0);
  }

  [Fact]
  public async Task should_persist_the_teacher_in_the_base_and_subtype_tables()
  {
    await AddAsync(Teacher.Create("Paulo", "paulo@classbooking.dev", "hash", CreatedAt));

    await AssertRowCountsAsync(users: 1, students: 0, teachers: 1);
  }

  [Fact]
  public async Task should_persist_the_admin_only_in_the_base_table()
  {
    await AddAsync(User.CreateAdmin("Root", "root@classbooking.dev", "hash", CreatedAt));

    await AssertRowCountsAsync(users: 1, students: 0, teachers: 0);
  }

  [Fact]
  public async Task should_persist_the_account_added_through_the_repository()
  {
    using (IServiceScope scope = CreateScope())
    {
      IUserRepository repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
      IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
      repository.Add(Student.Register("Ana", "ana@classbooking.dev", "hash", CreatedAt));
      await unitOfWork.SaveChangesAsync(default);
    }

    await AssertRowCountsAsync(users: 1, students: 1, teachers: 0);
  }

  [Fact]
  public async Task should_materialize_the_concrete_account_type()
  {
    Student student = Student.Register("Ana", "ana@classbooking.dev", "hash", CreatedAt);
    Teacher teacher = Teacher.Create("Paulo", "paulo@classbooking.dev", "hash", CreatedAt);
    User admin = User.CreateAdmin("Root", "root@classbooking.dev", "hash", CreatedAt);
    await AddAsync(student, teacher, admin);

    using IServiceScope scope = CreateScope();
    IUserRepository repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

    User? loadedStudent = await repository.GetByIdAsync(student.Id, default);
    User? loadedTeacher = await repository.GetByIdAsync(teacher.Id, default);
    User? loadedAdmin = await repository.GetByIdAsync(admin.Id, default);

    loadedStudent.Should().BeOfType<Student>().Which.Role.Should().Be(UserRole.Student);
    loadedTeacher.Should().BeOfType<Teacher>().Which.Role.Should().Be(UserRole.Teacher);
    loadedAdmin.Should().BeOfType<User>().Which.Role.Should().Be(UserRole.Admin);
    loadedStudent!.CreatedAt.Should().Be(CreatedAt);
  }

  [Fact]
  public async Task should_find_the_account_ignoring_case()
  {
    await AddAsync(Student.Register("Ana", "ana@classbooking.dev", "hash", CreatedAt));

    using IServiceScope scope = CreateScope();
    IUserRepository repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

    User? found = await repository.GetByEmailAsync("ANA@CLASSBOOKING.DEV", default);
    bool exists = await repository.ExistsByEmailAsync("ANA@CLASSBOOKING.DEV", default);

    found.Should().NotBeNull();
    found!.Email.Should().Be("ana@classbooking.dev");
    exists.Should().BeTrue();
  }

  [Fact]
  public async Task should_report_an_unknown_email_as_missing()
  {
    using IServiceScope scope = CreateScope();
    IUserRepository repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

    User? found = await repository.GetByEmailAsync("nobody@classbooking.dev", default);
    bool exists = await repository.ExistsByEmailAsync("nobody@classbooking.dev", default);

    found.Should().BeNull();
    exists.Should().BeFalse();
  }

  private async Task AssertRowCountsAsync(long users, long students, long teachers)
  {
    long actualUsers = await ScalarAsync<long>("SELECT COUNT(*) FROM users");
    long actualStudents = await ScalarAsync<long>("SELECT COUNT(*) FROM students");
    long actualTeachers = await ScalarAsync<long>("SELECT COUNT(*) FROM teachers");

    actualUsers.Should().Be(users);
    actualStudents.Should().Be(students);
    actualTeachers.Should().Be(teachers);
  }
}
