using ClassBooking.Api.Errors;
using ClassBooking.Application.Common;
using ClassBooking.Domain.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace ClassBooking.IntegrationTests.Api;

public sealed class ProblemDetailsMapperTests
{
  [Theory]
  [InlineData("ValidationFailed", 400)]
  [InlineData("ReasonRequired", 400)]
  [InlineData("InvalidAvailabilityRule", 400)]
  [InlineData("InvalidAvailabilityBlock", 400)]
  [InlineData("InvalidCredentials", 401)]
  [InlineData("PaymentDeclined", 402)]
  [InlineData("PermissionDenied", 403)]
  [InlineData("BookingNotFound", 404)]
  [InlineData("TeacherNotFound", 404)]
  [InlineData("UserNotFound", 404)]
  [InlineData("AvailabilityRuleNotFound", 404)]
  [InlineData("AvailabilityBlockNotFound", 404)]
  [InlineData("ResourceNotFound", 404)]
  [InlineData("SlotAlreadyBooked", 409)]
  [InlineData("StudentScheduleConflict", 409)]
  [InlineData("ActiveBookingLimitExceeded", 409)]
  [InlineData("InvalidStateTransition", 409)]
  [InlineData("EmailAlreadyInUse", 409)]
  [InlineData("BookingWindowViolation", 422)]
  [InlineData("CancellationNotAllowed", 422)]
  [InlineData("ConfirmationWindowExpired", 422)]
  [InlineData("NoShowWindowClosed", 422)]
  [InlineData("ReclassificationWindowClosed", 422)]
  [InlineData("RescheduleLimitReached", 422)]
  [InlineData("RescheduleNotAllowed", 422)]
  [InlineData("SlotNotAvailable", 422)]
  [InlineData("SelfDeactivationNotAllowed", 422)]
  [InlineData("RateLimitExceeded", 429)]
  [InlineData("UnexpectedError", 500)]
  public void should_map_error_code_to_canonical_http_status(string errorCode, int expectedStatus)
  {
    ProblemDetails problem = ProblemDetailsMapper.ToProblemDetails(new Error(errorCode, "Message."), null, null);

    problem.Status.Should().Be(expectedStatus);
    problem.Extensions["errorCode"].Should().Be(errorCode);
  }

  [Fact]
  public void should_map_unknown_error_code_to_internal_server_error()
  {
    ProblemDetails problem = ProblemDetailsMapper.ToProblemDetails(new Error("SomethingNew", "Message."), null, null);

    problem.Status.Should().Be(500);
    problem.Extensions["errorCode"].Should().Be("SomethingNew");
  }

  [Fact]
  public void should_build_type_uri_with_kebab_case_code()
  {
    ProblemDetails problem = ProblemDetailsMapper.ToProblemDetails(
        new Error("SlotAlreadyBooked", "The slot already has an active booking."), null, null);

    problem.Type.Should().Be("https://veidjz.github.io/class-booking-api/errors/slot-already-booked");
  }

  [Fact]
  public void should_humanize_code_into_title_and_use_message_as_detail()
  {
    ProblemDetails problem = ProblemDetailsMapper.ToProblemDetails(
        new Error("SlotAlreadyBooked", "The slot already has an active booking."), null, null);

    problem.Title.Should().Be("Slot already booked");
    problem.Detail.Should().Be("The slot already has an active booking.");
  }

  [Fact]
  public void should_carry_instance_and_trace_id_when_provided()
  {
    ProblemDetails problem = ProblemDetailsMapper.ToProblemDetails(
        new Error("PaymentDeclined", "The payment was declined."),
        "/api/v1/bookings/0198c0de-0000-7000-8000-000000000001/confirm",
        "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01");

    problem.Instance.Should().Be("/api/v1/bookings/0198c0de-0000-7000-8000-000000000001/confirm");
    problem.Extensions["traceId"].Should().Be("00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01");
  }

  [Fact]
  public void should_omit_trace_id_extension_when_absent()
  {
    ProblemDetails problem = ProblemDetailsMapper.ToProblemDetails(new Error("PaymentDeclined", "Message."), null, null);

    problem.Extensions.Should().NotContainKey("traceId");
  }

  [Fact]
  public void should_expose_field_errors_when_error_is_validation_error()
  {
    ValidationError validationError = new ValidationError(new Dictionary<string, string[]>
    {
      ["email"] = ["Email is required."],
      ["fullName"] = ["Full name is required."],
    });

    ProblemDetails problem = ProblemDetailsMapper.ToProblemDetails(validationError, null, null);

    problem.Status.Should().Be(400);
    problem.Title.Should().Be("Validation failed");
    problem.Detail.Should().Be("One or more validation errors occurred.");
    IReadOnlyDictionary<string, string[]> errors = problem.Extensions["errors"].Should()
        .BeAssignableTo<IReadOnlyDictionary<string, string[]>>().Subject;
    errors.Should().ContainKey("email");
    errors.Should().ContainKey("fullName");
  }
}
