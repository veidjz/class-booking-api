using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClassBooking.Api.Serialization;

internal sealed class UtcInstantJsonConverter : JsonConverter<DateTimeOffset>
{
  private const string Format = "yyyy-MM-ddTHH:mm:ssZ";

  private const DateTimeStyles ReadStyles = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;

  /// <remarks>
  /// An instant without an offset means UTC, not the time zone of whichever machine is running:
  /// the same payload has to mean the same instant everywhere the API is deployed.
  /// </remarks>
  public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    if (reader.TokenType is not JsonTokenType.String
        || !DateTimeOffset.TryParse(reader.GetString(), CultureInfo.InvariantCulture, ReadStyles, out DateTimeOffset instant))
    {
      throw new JsonException("Expected an ISO-8601 instant.");
    }

    return instant;
  }

  public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
      writer.WriteStringValue(value.UtcDateTime.ToString(Format, CultureInfo.InvariantCulture));
}
