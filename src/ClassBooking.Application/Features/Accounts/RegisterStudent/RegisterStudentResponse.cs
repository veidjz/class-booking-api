using ClassBooking.Domain.Users;

namespace ClassBooking.Application.Features.Accounts.RegisterStudent;

public sealed record RegisterStudentResponse(
    Guid Id,
    string Name,
    string Email,
    UserRole Role,
    bool Active,
    DateTimeOffset CreatedAt);
