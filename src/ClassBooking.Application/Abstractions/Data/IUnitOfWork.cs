using ClassBooking.Domain.Common;

namespace ClassBooking.Application.Abstractions.Data;

public interface IUnitOfWork
{
  Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken);

  IReadOnlyCollection<IDomainEvent> DequeueDomainEvents();

  Task<Result> SaveChangesAsync(CancellationToken cancellationToken);
}
