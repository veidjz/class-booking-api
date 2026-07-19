using System.Text.Json;
using ClassBooking.Api.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClassBooking.IntegrationTests.Api;

public sealed class GlobalExceptionHandlerTests
{
  [Fact]
  public async Task should_write_generic_500_problem_when_exception_is_unhandled()
  {
    await using ServiceProvider provider = new ServiceCollection()
        .AddLogging()
        .AddProblemDetails()
        .BuildServiceProvider();
    GlobalExceptionHandler handler = new GlobalExceptionHandler(
        NullLogger<GlobalExceptionHandler>.Instance,
        provider.GetRequiredService<IProblemDetailsService>());
    DefaultHttpContext httpContext = new DefaultHttpContext { RequestServices = provider };
    httpContext.Request.Path = "/api/v1/bookings";
    using MemoryStream body = new MemoryStream();
    httpContext.Response.Body = body;

    bool handled = await handler.TryHandleAsync(httpContext, new InvalidOperationException("secret detail"), CancellationToken.None);

    handled.Should().BeTrue();
    httpContext.Response.StatusCode.Should().Be(500);
    httpContext.Response.ContentType.Should().StartWith("application/problem+json");

    body.Position = 0;
    using JsonDocument problem = await JsonDocument.ParseAsync(body);
    problem.RootElement.GetProperty("errorCode").GetString().Should().Be("UnexpectedError");
    problem.RootElement.GetProperty("detail").GetString().Should().NotContain("secret detail");
  }
}
