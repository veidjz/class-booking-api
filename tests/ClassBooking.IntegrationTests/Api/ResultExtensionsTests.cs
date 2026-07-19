using ClassBooking.Api.Errors;
using ClassBooking.Domain.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ClassBooking.IntegrationTests.Api;

public sealed class ResultExtensionsTests
{
  [Fact]
  public void should_convert_failure_result_into_problem_result()
  {
    DefaultHttpContext httpContext = new DefaultHttpContext();
    httpContext.Request.Path = "/api/v1/bookings";
    Result result = Result.Failure(new Error("SlotAlreadyBooked", "The slot already has an active booking."));

    ProblemHttpResult problemResult = result.ToProblem(httpContext).Should().BeOfType<ProblemHttpResult>().Subject;

    problemResult.StatusCode.Should().Be(409);
    problemResult.ProblemDetails.Instance.Should().Be("/api/v1/bookings");
    problemResult.ProblemDetails.Extensions["errorCode"].Should().Be("SlotAlreadyBooked");
    problemResult.ProblemDetails.Extensions["traceId"].Should().NotBeNull();
  }

  [Fact]
  public void should_throw_when_mapping_success_result_to_problem()
  {
    Func<IResult> act = () => Result.Success().ToProblem(new DefaultHttpContext());

    act.Should().Throw<InvalidOperationException>();
  }
}
