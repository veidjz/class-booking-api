using System.Text.Json;
using ClassBooking.Api.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClassBooking.IntegrationTests.Api;

public sealed class GlobalExceptionHandlerTests
{
  [Fact]
  public async Task should_write_generic_500_problem_when_exception_is_unhandled()
  {
    var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
    var httpContext = new DefaultHttpContext();
    httpContext.Request.Path = "/api/v1/bookings";
    using var body = new MemoryStream();
    httpContext.Response.Body = body;

    var handled = await handler.TryHandleAsync(httpContext, new InvalidOperationException("secret detail"), CancellationToken.None);

    handled.Should().BeTrue();
    httpContext.Response.StatusCode.Should().Be(500);
    httpContext.Response.ContentType.Should().StartWith("application/problem+json");

    body.Position = 0;
    using var problem = await JsonDocument.ParseAsync(body);
    problem.RootElement.GetProperty("errorCode").GetString().Should().Be("UnexpectedError");
    problem.RootElement.GetProperty("detail").GetString().Should().NotContain("secret detail");
  }
}
