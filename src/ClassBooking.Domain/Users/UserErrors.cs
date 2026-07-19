using ClassBooking.Domain.Common;

namespace ClassBooking.Domain.Users;

public static class UserErrors
{
  public static readonly Error EmailAlreadyInUse =
      new Error("EmailAlreadyInUse", "The email already belongs to an account.");

  public static readonly Error UserNotFound =
      new Error("UserNotFound", "The account was not found.");

  public static readonly Error SelfDeactivationNotAllowed =
      new Error("SelfDeactivationNotAllowed", "An admin cannot change their own activation state.");
}
