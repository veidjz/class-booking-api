using System.Data;
using ClassBooking.Application.Abstractions.Data;
using ClassBooking.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;

namespace ClassBooking.Infrastructure.Persistence;

internal sealed class UnitOfWork(AppDbContext context) : IUnitOfWork
{
  public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
  {
    IDbContextTransaction transaction =
        await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

    return new UnitOfWorkTransaction(transaction);
  }

  public IReadOnlyCollection<IDomainEvent> DequeueDomainEvents()
  {
    List<IDomainEvent> domainEvents = [];
    foreach (EntityEntry<AggregateRoot> entry in context.ChangeTracker.Entries<AggregateRoot>())
    {
      domainEvents.AddRange(entry.Entity.DomainEvents);
      entry.Entity.ClearDomainEvents();
    }

    return domainEvents;
  }

  public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
  {
    await context.SaveChangesAsync(cancellationToken);

    return Result.Success();
  }
}
