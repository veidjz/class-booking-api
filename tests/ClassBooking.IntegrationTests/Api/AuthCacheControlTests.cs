using System.Net.Http.Json;
using ClassBooking.IntegrationTests.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ClassBooking.IntegrationTests.Api;

public sealed class AuthCacheControlTests : IDisposable
{
  private readonly WebApplicationFactory<Program> _root = new WebApplicationFactory<Program>();
  private readonly WebApplicationFactory<Program> _factory;

  public AuthCacheControlTests() => _factory = _root.Configure(ApiHost.UnusedConnectionString);

  public void Dispose() => _root.Dispose();

  [Theory]
  [InlineData("/api/v1/auth/register")]
  [InlineData("/api/v1/auth/login")]
  public async Task should_mark_every_auth_response_as_non_cacheable(string route)
  {
    using HttpClient client = _factory.CreateClient();

    using HttpResponseMessage response = await client.PostAsJsonAsync(route, new { });

    response.Headers.CacheControl!.NoStore.Should().BeTrue();
  }
}
