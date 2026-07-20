using ClassBooking.Api.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ClassBooking.IntegrationTests.Api;

public sealed class UnmatchedRouteProblemTests
{
  [Fact]
  public void should_shape_a_not_found_that_matched_no_route()
  {
    HttpContext httpContext = Context(StatusCodes.Status404NotFound, matched: false);

    UnmatchedRouteProblem.ShouldShape(httpContext).Should().BeTrue();
  }

  [Fact]
  public void should_leave_a_not_found_chosen_by_an_endpoint()
  {
    HttpContext httpContext = Context(StatusCodes.Status404NotFound, matched: true);

    UnmatchedRouteProblem.ShouldShape(httpContext).Should().BeFalse();
  }

  [Theory]
  [InlineData(StatusCodes.Status400BadRequest)]
  [InlineData(StatusCodes.Status405MethodNotAllowed)]
  [InlineData(StatusCodes.Status415UnsupportedMediaType)]
  public void should_leave_the_statuses_the_framework_answers_on_its_own(int statusCode)
  {
    HttpContext httpContext = Context(statusCode, matched: false);

    UnmatchedRouteProblem.ShouldShape(httpContext).Should().BeFalse();
  }

  private static HttpContext Context(int statusCode, bool matched)
  {
    DefaultHttpContext httpContext = new DefaultHttpContext();
    httpContext.Response.StatusCode = statusCode;

    if (matched)
    {
      httpContext.SetEndpoint(new Endpoint(_ => Task.CompletedTask, EndpointMetadataCollection.Empty, "test"));
    }

    return httpContext;
  }
}
