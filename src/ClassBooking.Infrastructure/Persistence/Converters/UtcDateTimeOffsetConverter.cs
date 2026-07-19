using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ClassBooking.Infrastructure.Persistence.Converters;

public sealed class UtcDateTimeOffsetConverter : ValueConverter<DateTimeOffset, DateTime>
{
  public UtcDateTimeOffsetConverter()
      : base(
          value => value.UtcDateTime,
          value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)))
  {
  }
}
