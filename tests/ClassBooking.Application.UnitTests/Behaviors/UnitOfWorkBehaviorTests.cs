using ClassBooking.Application.Abstractions.Data;
using ClassBooking.Application.Abstractions.Messaging;
using ClassBooking.Application.Behaviors;
using ClassBooking.Domain.Common;
using FluentAssertions;
using MediatR;

namespace ClassBooking.Application.UnitTests.Behaviors;

public sealed class UnitOfWorkBehaviorTests
{
  private sealed record CreateBookingCommand(Guid StudentId) : ICommand<Guid>;

  private sealed record BookingCreatedDomainEvent(Guid BookingId) : IDomainEvent;

  private sealed record ReminderScheduledDomainEvent(Guid BookingId) : IDomainEvent;

  private static readonly Error SomeError = new("SlotAlreadyBooked", "The slot already has an active booking.");

  private readonly FakeUnitOfWork _unitOfWork = new();
  private readonly RecordingPublisher _publisher = new();

  private UnitOfWorkBehavior<CreateBookingCommand, Result<Guid>> CreateBehavior() => new(_unitOfWork, _publisher);

  private static Task<Result<Guid>> SuccessHandler(CancellationToken cancellationToken = default) =>
      Task.FromResult(Result.Success(Guid.CreateVersion7()));

  [Fact]
  public async Task should_commit_transaction_when_handler_and_save_succeed()
  {
    Result<Guid> response = await CreateBehavior().Handle(new CreateBookingCommand(Guid.CreateVersion7()), SuccessHandler, CancellationToken.None);

    response.IsSuccess.Should().BeTrue();
    _unitOfWork.Transaction.Should().NotBeNull();
    _unitOfWork.Transaction!.Committed.Should().BeTrue();
    _unitOfWork.Transaction.Disposed.Should().BeTrue();
    _unitOfWork.Calls.Should().ContainInOrder("begin", "save", "commit");
  }

  [Fact]
  public async Task should_roll_back_without_saving_when_handler_returns_failure()
  {
    Result<Guid> response = await CreateBehavior().Handle(
        new CreateBookingCommand(Guid.CreateVersion7()),
        _ => Task.FromResult(Result.Failure<Guid>(SomeError)),
        CancellationToken.None);

    response.IsFailure.Should().BeTrue();
    response.Error.Should().Be(SomeError);
    _unitOfWork.SaveCalls.Should().Be(0);
    _unitOfWork.Transaction!.Committed.Should().BeFalse();
    _unitOfWork.Transaction.Disposed.Should().BeTrue();
  }

  [Fact]
  public async Task should_return_save_failure_without_committing_when_save_fails()
  {
    _unitOfWork.SaveResult = Result.Failure(SomeError);

    Result<Guid> response = await CreateBehavior().Handle(new CreateBookingCommand(Guid.CreateVersion7()), SuccessHandler, CancellationToken.None);

    response.IsFailure.Should().BeTrue();
    response.Error.Should().Be(SomeError);
    _unitOfWork.Transaction!.Committed.Should().BeFalse();
    _unitOfWork.Transaction.Disposed.Should().BeTrue();
  }

  [Fact]
  public async Task should_publish_domain_events_before_saving_when_handler_succeeds()
  {
    BookingCreatedDomainEvent first = new BookingCreatedDomainEvent(Guid.CreateVersion7());
    _unitOfWork.EventBatches.Enqueue([first]);

    await CreateBehavior().Handle(new CreateBookingCommand(Guid.CreateVersion7()), SuccessHandler, CancellationToken.None);

    _publisher.Published.Should().Equal(first);
    _unitOfWork.Calls.Should().ContainInOrder("begin", "dequeue", "save", "commit");
  }

  [Fact]
  public async Task should_keep_publishing_when_event_handlers_raise_cascading_events()
  {
    BookingCreatedDomainEvent first = new BookingCreatedDomainEvent(Guid.CreateVersion7());
    ReminderScheduledDomainEvent cascaded = new ReminderScheduledDomainEvent(first.BookingId);
    _unitOfWork.EventBatches.Enqueue([first]);
    _unitOfWork.EventBatches.Enqueue([cascaded]);

    await CreateBehavior().Handle(new CreateBookingCommand(Guid.CreateVersion7()), SuccessHandler, CancellationToken.None);

    _publisher.Published.Should().Equal(first, cascaded);
  }

  [Fact]
  public async Task should_not_publish_events_when_handler_returns_failure()
  {
    _unitOfWork.EventBatches.Enqueue([new BookingCreatedDomainEvent(Guid.CreateVersion7())]);

    await CreateBehavior().Handle(
        new CreateBookingCommand(Guid.CreateVersion7()),
        _ => Task.FromResult(Result.Failure<Guid>(SomeError)),
        CancellationToken.None);

    _publisher.Published.Should().BeEmpty();
  }

  [Fact]
  public async Task should_roll_back_and_propagate_when_handler_throws()
  {
    Func<Task<Result<Guid>>> act = () => CreateBehavior().Handle(
        new CreateBookingCommand(Guid.CreateVersion7()),
        _ => throw new InvalidOperationException("boom"),
        CancellationToken.None);

    await act.Should().ThrowAsync<InvalidOperationException>();
    _unitOfWork.SaveCalls.Should().Be(0);
    _unitOfWork.Transaction!.Committed.Should().BeFalse();
    _unitOfWork.Transaction.Disposed.Should().BeTrue();
  }

  private sealed class FakeUnitOfWork : IUnitOfWork
  {
    internal List<string> Calls { get; } = [];

    internal Queue<IDomainEvent[]> EventBatches { get; } = new();

    internal FakeTransaction? Transaction { get; private set; }

    internal Result SaveResult { get; set; } = Result.Success();

    internal int SaveCalls { get; private set; }

    public Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
      Calls.Add("begin");
      Transaction = new FakeTransaction(Calls);
      return Task.FromResult<IUnitOfWorkTransaction>(Transaction);
    }

    public IReadOnlyCollection<IDomainEvent> DequeueDomainEvents()
    {
      Calls.Add("dequeue");
      return EventBatches.TryDequeue(out IDomainEvent[]? batch) ? batch : [];
    }

    public Task<Result> SaveChangesAsync(CancellationToken cancellationToken)
    {
      Calls.Add("save");
      SaveCalls++;
      return Task.FromResult(SaveResult);
    }
  }

  private sealed class FakeTransaction(List<string> calls) : IUnitOfWorkTransaction
  {
    internal bool Committed { get; private set; }

    internal bool Disposed { get; private set; }

    public Task CommitAsync(CancellationToken cancellationToken)
    {
      calls.Add("commit");
      Committed = true;
      return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
      Disposed = true;
      return ValueTask.CompletedTask;
    }
  }

  private sealed class RecordingPublisher : IPublisher
  {
    internal List<object> Published { get; } = [];

    public Task Publish(object notification, CancellationToken cancellationToken = default)
    {
      Published.Add(notification);
      return Task.CompletedTask;
    }

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
      Published.Add(notification);
      return Task.CompletedTask;
    }
  }
}
