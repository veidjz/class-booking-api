using ClassBooking.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ClassBooking.Infrastructure.Persistence.Interceptors;

internal sealed class VersionTokenInterceptor : SaveChangesInterceptor
{
  public override InterceptionResult<int> SavingChanges(
      DbContextEventData eventData,
      InterceptionResult<int> result)
  {
    BumpVersions(eventData.Context);

    return base.SavingChanges(eventData, result);
  }

  public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
      DbContextEventData eventData,
      InterceptionResult<int> result,
      CancellationToken cancellationToken = default)
  {
    BumpVersions(eventData.Context);

    return base.SavingChangesAsync(eventData, result, cancellationToken);
  }

  private static void BumpVersions(DbContext? context)
  {
    if (context is null)
    {
      return;
    }

    foreach (EntityEntry<AggregateRoot> entry in context.ChangeTracker.Entries<AggregateRoot>())
    {
      if (entry.State != EntityState.Modified)
      {
        continue;
      }

      PropertyEntry<AggregateRoot, int> version = entry.Property<int>("Version");
      version.CurrentValue = version.OriginalValue + 1;
    }
  }
}
