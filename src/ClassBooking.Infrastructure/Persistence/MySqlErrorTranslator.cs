using ClassBooking.Domain.Common;

namespace ClassBooking.Infrastructure.Persistence;

internal static class MySqlErrorTranslator
{
  private const string KeyMarker = "for key '";

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

    return UniqueIndexes.Errors.GetValueOrDefault(indexName);
  }
}
