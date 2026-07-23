using ClassBooking.Domain.Common;

namespace ClassBooking.Application.Common;

/// <remarks>
/// Transport-category code, in the same family as <see cref="ValidationError" />: it never comes
/// from a business rule, and one uniform message covers every way a login can fail.
/// </remarks>
public static class AuthenticationErrors
{
  public static readonly Error InvalidCredentials =
      new Error("InvalidCredentials", "The email or password is incorrect.");
}
