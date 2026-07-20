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
  public async Task<Result<RegisterStudentResponse>> Handle(
      RegisterStudentCommand command,
      CancellationToken cancellationToken)
  {
    DateTimeOffset now = clock.GetUtcNow();
    string name = command.Name.Trim();
    string email = command.Email.Trim().ToLowerInvariant();

    if (await users.ExistsByEmailAsync(email, cancellationToken))
    {
      return Result.Failure<RegisterStudentResponse>(UserErrors.EmailAlreadyInUse);
    }

    string passwordHash = passwordHasher.Hash(command.Password);

    Student student = Student.Register(name, email, passwordHash, now);
    users.Add(student);

    return Result.Success(new RegisterStudentResponse(
        student.Id,
        student.Name,
        student.Email,
        student.Role,
        student.IsActive,
        student.CreatedAt));
  }
}
