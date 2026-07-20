using ClassBooking.Application.Abstractions.Messaging;

namespace ClassBooking.Application.Features.Accounts.RegisterStudent;

public sealed record RegisterStudentCommand(string Name, string Email, string Password)
    : ICommand<RegisterStudentResponse>;
