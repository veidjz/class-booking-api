using ClassBooking.Application.Abstractions.Messaging;
using ClassBooking.Application.Behaviors;
using ClassBooking.Application.UnitTests.Fakes;
using ClassBooking.Domain.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace ClassBooking.Application.UnitTests.Behaviors;

public sealed class RequestLoggingBehaviorTests
{
  private sealed record ConfirmBookingCommand(Guid BookingId) : ICommand;

  private static readonly Error SomeError = new("ConfirmationWindowExpired", "The confirmation window has expired.");

  private readonly CapturingLogger<RequestLoggingBehavior<ConfirmBookingCommand, Result>> _logger = new();
  private readonly FakeTimeProvider _clock = new();

  private RequestLoggingBehavior<ConfirmBookingCommand, Result> CreateBehavior() => new(_logger, _clock);

  [Fact]
  public async Task should_log_start_and_completion_as_information_when_handler_succeeds()
  {
    Result response = await CreateBehavior().Handle(
        new ConfirmBookingCommand(Guid.CreateVersion7()),
        _ =>
        {
          _clock.Advance(TimeSpan.FromMilliseconds(150));
          return Task.FromResult(Result.Success());
        },
        CancellationToken.None);

    response.IsSuccess.Should().BeTrue();
    _logger.Entries.Should().HaveCount(2);
    _logger.Entries[0].Level.Should().Be(LogLevel.Information);
    _logger.Entries[0].Message.Should().Be("Handling ConfirmBookingCommand");
    _logger.Entries[1].Level.Should().Be(LogLevel.Information);
    _logger.Entries[1].Properties.Should().Contain("RequestName", "ConfirmBookingCommand");
    _logger.Entries[1].Properties.Should().Contain("ElapsedMilliseconds", 150d);
  }

  [Fact]
  public async Task should_log_error_code_as_information_when_handler_returns_failure()
  {
    Result response = await CreateBehavior().Handle(
        new ConfirmBookingCommand(Guid.CreateVersion7()),
        _ => Task.FromResult(Result.Failure(SomeError)),
        CancellationToken.None);

    response.IsFailure.Should().BeTrue();
    _logger.Entries.Should().HaveCount(2);
    _logger.Entries[1].Level.Should().Be(LogLevel.Information);
    _logger.Entries[1].Properties.Should().Contain("ErrorCode", "ConfirmationWindowExpired");
    _logger.Entries[1].Properties.Should().Contain("RequestName", "ConfirmBookingCommand");
  }

  [Fact]
  public async Task should_push_request_name_into_log_scope()
  {
    await CreateBehavior().Handle(
        new ConfirmBookingCommand(Guid.CreateVersion7()),
        _ => Task.FromResult(Result.Success()),
        CancellationToken.None);

    IReadOnlyDictionary<string, object> scope = _logger.Scopes.Should().ContainSingle().Subject
        .Should().BeAssignableTo<IReadOnlyDictionary<string, object>>().Subject;
    scope.Should().Contain("RequestName", "ConfirmBookingCommand");
  }

  [Fact]
  public async Task should_propagate_exception_without_logging_error_when_handler_throws()
  {
    Func<Task<Result>> act = () => CreateBehavior().Handle(
        new ConfirmBookingCommand(Guid.CreateVersion7()),
        _ => throw new InvalidOperationException("boom"),
        CancellationToken.None);

    await act.Should().ThrowAsync<InvalidOperationException>();
    _logger.Entries.Should().ContainSingle();
    _logger.Entries[0].Message.Should().Be("Handling ConfirmBookingCommand");
  }
}
