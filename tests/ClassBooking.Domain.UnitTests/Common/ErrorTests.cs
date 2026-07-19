using ClassBooking.Domain.Common;
using FluentAssertions;

namespace ClassBooking.Domain.UnitTests.Common;

public sealed class ErrorTests
{
  [Fact]
  public void should_expose_code_and_message()
  {
    Error error = new Error("SlotAlreadyBooked", "The slot already has an active booking.");

    error.Code.Should().Be("SlotAlreadyBooked");
    error.Message.Should().Be("The slot already has an active booking.");
  }

  [Fact]
  public void should_be_equal_when_code_and_message_match()
  {
    Error first = new Error("SlotAlreadyBooked", "The slot already has an active booking.");
    Error second = new Error("SlotAlreadyBooked", "The slot already has an active booking.");

    first.Should().Be(second);
  }

  [Fact]
  public void should_have_empty_code_and_message_when_none()
  {
    Error.None.Code.Should().BeEmpty();
    Error.None.Message.Should().BeEmpty();
  }
}
