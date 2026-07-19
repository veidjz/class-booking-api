using ClassBooking.Domain.Common;
using ClassBooking.Domain.Users;

namespace ClassBooking.Infrastructure.Persistence;

internal static class MySqlErrorTranslator
{
  private const string KeyMarker = "for key '";

  private static readonly Dictionary<string, Error> ErrorsByIndex =
      new(StringComparer.OrdinalIgnoreCase) { ["ux_users_email"] = UserErrors.EmailAlreadyInUse };

  public static Error? TranslateDuplicateKey(string message)
  {
    int markerIndex = message.IndexOf(KeyMarker, StringComparison.OrdinalIgnoreCase);
    if (markerIndex < 0)
    {
      return null;
    }

    int start = markerIndex + KeyMarker.Length;
    int end = message.IndexOf('\'', start);
    if (end < 0)
    {
      return null;
    }

    string key = message[start..end];
    int separatorIndex = key.LastIndexOf('.');
    string indexName = separatorIndex < 0 ? key : key[(separatorIndex + 1)..];

    return ErrorsByIndex.GetValueOrDefault(indexName);
  }
}
