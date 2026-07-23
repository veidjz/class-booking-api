namespace ClassBooking.Application.Features.Accounts.Login;

public sealed record LoginResponse(string TokenType, string AccessToken, int ExpiresIn);
