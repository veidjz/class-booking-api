using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClassBooking.IntegrationTests.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ClassBooking.IntegrationTests.Api;

public sealed class RateLimitTests : IDisposable
{
  private const string Route = "/api/v1/auth/register";

  private readonly WebApplicationFactory<Program> _root = new WebApplicationFactory<Program>();
  private readonly WebApplicationFactory<Program> _factory;

  public RateLimitTests() =>
      _factory = _root.Configure(ApiHost.UnusedConnectionString, "Production", rateLimiting: true);

  public void Dispose() => _root.Dispose();

  [Fact]
  public async Task should_throttle_the_registration_route_after_five_attempts()
  {
    using HttpClient client = _factory.CreateClient();

    for (int attempt = 1; attempt <= 5; attempt++)
    {
      using HttpResponseMessage allowed = await PostAsync(client);
      allowed.StatusCode.Should().Be(HttpStatusCode.BadRequest, "attempt {0} is within the limit", attempt);
    }

    using HttpResponseMessage rejected = await PostAsync(client);

    rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    rejected.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    rejected.Headers.RetryAfter!.Delta.Should().Be(TimeSpan.FromMinutes(1));

    using JsonDocument problem = JsonDocument.Parse(await rejected.Content.ReadAsStringAsync());
    problem.RootElement.GetProperty("errorCode").GetString().Should().Be("RateLimitExceeded");
    problem.RootElement.GetProperty("status").GetInt32().Should().Be(429);
    problem.RootElement.GetProperty("instance").GetString().Should().Be(Route);
  }

  [Fact]
  public async Task should_throttle_any_route_after_one_hundred_requests()
  {
    using HttpClient client = _factory.CreateClient();

    for (int attempt = 1; attempt <= 100; attempt++)
    {
      using HttpResponseMessage allowed = await client.GetAsync("/api/v1/definitely-missing");
      allowed.StatusCode.Should().Be(HttpStatusCode.NotFound, "attempt {0} is within the limit", attempt);
    }

    using HttpResponseMessage rejected = await client.GetAsync("/api/v1/definitely-missing");

    rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

    using JsonDocument problem = JsonDocument.Parse(await rejected.Content.ReadAsStringAsync());
    problem.RootElement.GetProperty("errorCode").GetString().Should().Be("RateLimitExceeded");
  }

  [Fact]
  public async Task should_throttle_the_login_route_after_five_attempts()
  {
    const string loginRoute = "/api/v1/auth/login";
    using HttpClient client = _factory.CreateClient();

    for (int attempt = 1; attempt <= 5; attempt++)
    {
      using HttpResponseMessage allowed = await client.PostAsJsonAsync(loginRoute, new { email = "", password = "" });
      allowed.StatusCode.Should().Be(HttpStatusCode.BadRequest, "attempt {0} is within the limit", attempt);
    }

    using HttpResponseMessage rejected = await client.PostAsJsonAsync(loginRoute, new { email = "", password = "" });

    rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    rejected.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    rejected.Headers.RetryAfter!.Delta.Should().Be(TimeSpan.FromMinutes(1));

    using JsonDocument problem = JsonDocument.Parse(await rejected.Content.ReadAsStringAsync());
    problem.RootElement.GetProperty("errorCode").GetString().Should().Be("RateLimitExceeded");
    problem.RootElement.GetProperty("status").GetInt32().Should().Be(429);
    problem.RootElement.GetProperty("instance").GetString().Should().Be(loginRoute);
  }

  [Fact]
  public async Task should_not_throttle_when_the_limiter_is_disabled()
  {
    using WebApplicationFactory<Program> disabled =
        _root.Configure(ApiHost.UnusedConnectionString, "Production");
    using HttpClient client = disabled.CreateClient();

    for (int attempt = 1; attempt <= 6; attempt++)
    {
      using HttpResponseMessage response = await PostAsync(client);
      response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "attempt {0} is never throttled", attempt);
    }
  }

  /// <summary>Posts a payload that validation rejects, so the request never reaches the database.</summary>
  private static Task<HttpResponseMessage> PostAsync(HttpClient client) =>
      client.PostAsJsonAsync(Route, new { name = "", email = "", password = "" });
}
