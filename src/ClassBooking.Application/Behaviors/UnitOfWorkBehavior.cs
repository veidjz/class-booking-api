using ClassBooking.Application.Abstractions.Data;
using ClassBooking.Application.Abstractions.Messaging;
using ClassBooking.Application.Common;
using ClassBooking.Domain.Common;
using MediatR;

namespace ClassBooking.Application.Behaviors;

internal sealed class UnitOfWorkBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork, IPublisher publisher)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseCommand
    where TResponse : Result
{
  public async Task<TResponse> Handle(
      TRequest request,
      RequestHandlerDelegate<TResponse> next,
      CancellationToken cancellationToken)
  {
    await using IUnitOfWorkTransaction transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

    TResponse response = await next(cancellationToken);
    if (response.IsFailure)
    {
      return response;
    }

    await PublishDomainEventsAsync(cancellationToken);

    Result saveResult = await unitOfWork.SaveChangesAsync(cancellationToken);
    if (saveResult.IsFailure)
    {
      return ResultFactory.Failure<TResponse>(saveResult.Error);
    }

    await transaction.CommitAsync(cancellationToken);
    return response;
  }

  private async Task PublishDomainEventsAsync(CancellationToken cancellationToken)
  {
    IReadOnlyCollection<IDomainEvent> domainEvents = unitOfWork.DequeueDomainEvents();
    while (domainEvents.Count > 0)
    {
      foreach (IDomainEvent domainEvent in domainEvents)
      {
        await publisher.Publish(domainEvent, cancellationToken);
      }

      domainEvents = unitOfWork.DequeueDomainEvents();
    }
  }
}
