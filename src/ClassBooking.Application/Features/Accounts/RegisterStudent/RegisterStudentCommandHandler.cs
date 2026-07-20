using ClassBooking.Application.Abstractions.Auth;
using ClassBooking.Application.Abstractions.Messaging;
using ClassBooking.Domain.Common;
using ClassBooking.Domain.Users;

namespace ClassBooking.Application.Features.Accounts.RegisterStudent;

internal sealed class RegisterStudentCommandHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    TimeProvider clock)
    : ICommandHandler<RegisterStudentCommand, RegisterStudentResponse>
{
  public Task<Result<RegisterStudentResponse>> Handle(
      RegisterStudentCommand command,
      CancellationToken cancellationToken)
  {
    DateTimeOffset now = clock.GetUtcNow();
    string passwordHash = passwordHasher.Hash(command.Password);

    Student student = Student.Register(command.Name, command.Email, passwordHash, now);
    users.Add(student);

    return Task.FromResult(Result.Success(new RegisterStudentResponse(
        student.Id,
        student.Name,
        student.Email,
        student.Role,
        student.IsActive,
        student.CreatedAt)));
  }
}
