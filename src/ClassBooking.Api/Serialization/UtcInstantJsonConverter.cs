using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClassBooking.Api.Serialization;

internal sealed class UtcInstantJsonConverter : JsonConverter<DateTimeOffset>
{
  private const string Format = "yyyy-MM-ddTHH:mm:ss.FFFFFFZ";

  public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
      reader.GetDateTimeOffset();

  public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
      writer.WriteStringValue(value.UtcDateTime.ToString(Format, CultureInfo.InvariantCulture));
}
