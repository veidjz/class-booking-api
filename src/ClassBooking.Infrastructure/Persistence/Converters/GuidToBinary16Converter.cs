using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ClassBooking.Infrastructure.Persistence.Converters;

public sealed class GuidToBinary16Converter : ValueConverter<Guid, byte[]>
{
  public GuidToBinary16Converter()
      : base(value => value.ToByteArray(true), value => new Guid(value, true))
  {
  }
}
