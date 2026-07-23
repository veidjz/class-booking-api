namespace ClassBooking.Application.Abstractions.Auth;

public sealed record AccessToken(string Token, int ExpiresInSeconds);
