using ClassBooking.Domain.Common;
using FluentAssertions;

namespace ClassBooking.Domain.UnitTests.Common;

public sealed class AggregateRootTests
{
  private sealed record SomethingHappenedDomainEvent(Guid AggregateId) : IDomainEvent;

  private sealed class SampleAggregate(Guid id) : AggregateRoot(id)
  {
    public void DoSomething() => Raise(new SomethingHappenedDomainEvent(Id));
  }

  [Fact]
  public void should_have_no_domain_events_when_created()
  {
    SampleAggregate aggregate = new SampleAggregate(Guid.CreateVersion7());

    aggregate.DomainEvents.Should().BeEmpty();
  }

  [Fact]
  public void should_accumulate_domain_events_when_raised()
  {
    SampleAggregate aggregate = new SampleAggregate(Guid.CreateVersion7());

    aggregate.DoSomething();
    aggregate.DoSomething();

    aggregate.DomainEvents.Should().HaveCount(2);
    aggregate.DomainEvents.Should().AllBeOfType<SomethingHappenedDomainEvent>();
  }

  [Fact]
  public void should_have_no_domain_events_when_cleared()
  {
    SampleAggregate aggregate = new SampleAggregate(Guid.CreateVersion7());
    aggregate.DoSomething();

    aggregate.ClearDomainEvents();

    aggregate.DomainEvents.Should().BeEmpty();
  }
}
