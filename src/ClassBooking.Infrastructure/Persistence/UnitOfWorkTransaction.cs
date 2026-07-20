using ClassBooking.Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace ClassBooking.Infrastructure.Persistence;

internal sealed class UnitOfWorkTransaction(IDbContextTransaction transaction) : IUnitOfWorkTransaction
{
  public Task CommitAsync(CancellationToken cancellationToken) => transaction.CommitAsync(cancellationToken);

  public ValueTask DisposeAsync() => transaction.DisposeAsync();
}
