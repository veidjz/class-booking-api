using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ClassBooking.Infrastructure.Persistence.Converters;

public sealed class GuidToBinary16Comparer : ValueComparer<Guid>
{
  public GuidToBinary16Comparer()
      : base((left, right) => left == right, value => value.GetHashCode())
  {
  }
}
