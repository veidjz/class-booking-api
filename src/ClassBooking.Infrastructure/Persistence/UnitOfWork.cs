using System.Data;
using ClassBooking.Application.Abstractions.Data;
using ClassBooking.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using MySqlConnector;

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
    try
    {
      await context.SaveChangesAsync(cancellationToken);

      return Result.Success();
    }
    catch (DbUpdateException exception) when (exception.InnerException is MySqlException { Number: 1062 } duplicate)
    {
      Error? error = MySqlErrorTranslator.TranslateDuplicateKey(duplicate.Message);
      if (error is null)
      {
        throw;
      }

      return Result.Failure(error);
    }
  }
}
