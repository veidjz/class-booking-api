namespace ClassBooking.Domain.Common;

public abstract class AggregateRoot(Guid id) : Entity(id)
{
  private readonly List<IDomainEvent> _domainEvents = [];

  public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

  public void ClearDomainEvents() => _domainEvents.Clear();

  protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
