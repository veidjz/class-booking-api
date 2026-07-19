using ClassBooking.Domain.Common;
using FluentAssertions;

namespace ClassBooking.Domain.UnitTests.Common;

public sealed class ResultTests
{
  private static readonly Error SomeError = new("SlotAlreadyBooked", "The slot already has an active booking.");

  [Fact]
  public void should_be_success_without_error_when_created_with_success()
  {
    Result result = Result.Success();

    result.IsSuccess.Should().BeTrue();
    result.IsFailure.Should().BeFalse();
    result.Error.Should().Be(Error.None);
  }

  [Fact]
  public void should_be_failure_with_error_when_created_with_failure()
  {
    Result result = Result.Failure(SomeError);

    result.IsSuccess.Should().BeFalse();
    result.IsFailure.Should().BeTrue();
    result.Error.Should().Be(SomeError);
  }

  [Fact]
  public void should_throw_when_failure_created_without_error()
  {
    Func<Result> act = () => Result.Failure(Error.None);

    act.Should().Throw<ArgumentException>();
  }

  [Fact]
  public void should_expose_value_when_generic_success()
  {
    Result<int> result = Result.Success(42);

    result.IsSuccess.Should().BeTrue();
    result.Value.Should().Be(42);
  }

  [Fact]
  public void should_throw_when_value_accessed_on_generic_failure()
  {
    Result<int> result = Result.Failure<int>(SomeError);

    result.IsFailure.Should().BeTrue();
    Func<int> act = () => result.Value;
    act.Should().Throw<InvalidOperationException>();
  }

  [Fact]
  public void should_convert_value_to_success_result_implicitly()
  {
    Result<int> result = 42;

    result.IsSuccess.Should().BeTrue();
    result.Value.Should().Be(42);
  }

  [Fact]
  public void should_throw_when_generic_failure_created_without_error()
  {
    Func<Result<int>> act = () => Result.Failure<int>(Error.None);

    act.Should().Throw<ArgumentException>();
  }
}
