namespace ClassBooking.Api.Endpoints.Auth;

public sealed record RegisterStudentRequest(string Name, string Email, string Password);
