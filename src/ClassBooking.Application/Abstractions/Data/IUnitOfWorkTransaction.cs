namespace ClassBooking.Application.Abstractions.Data;

public interface IUnitOfWorkTransaction : IAsyncDisposable
{
  Task CommitAsync(CancellationToken cancellationToken);
}
