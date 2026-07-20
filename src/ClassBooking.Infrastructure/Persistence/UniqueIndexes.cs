using ClassBooking.Domain.Common;
using ClassBooking.Domain.Users;

namespace ClassBooking.Infrastructure.Persistence;

internal static class UniqueIndexes
{
  public const string UsersEmail = "ux_users_email";

  public static readonly IReadOnlyDictionary<string, Error> Errors =
      new Dictionary<string, Error>(StringComparer.OrdinalIgnoreCase)
      {
        [UsersEmail] = UserErrors.EmailAlreadyInUse,
      };
}
