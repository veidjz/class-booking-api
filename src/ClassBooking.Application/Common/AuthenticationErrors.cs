using ClassBooking.Domain.Common;

namespace ClassBooking.Application.Common;

public static class AuthenticationErrors
{
  public static readonly Error InvalidCredentials =
      new Error("InvalidCredentials", "The email or password is incorrect.");
}
