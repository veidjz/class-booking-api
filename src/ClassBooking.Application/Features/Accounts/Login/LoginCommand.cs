using ClassBooking.Application.Abstractions.Messaging;

namespace ClassBooking.Application.Features.Accounts.Login;

public sealed record LoginCommand(string Email, string Password) : ICommand<LoginResponse>;
