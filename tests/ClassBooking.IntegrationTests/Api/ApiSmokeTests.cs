using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ClassBooking.IntegrationTests.Api;

public sealed class ApiSmokeTests
{
  private static WebApplicationFactory<Program> CreateFactory(string environment) =>
      new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseEnvironment(environment));

  [Fact]
  public async Task should_return_resource_not_found_problem_when_route_is_unknown()
  {
    using var factory = CreateFactory("Production");
    using var client = factory.CreateClient();

    using var response = await client.GetAsync("/api/v1/definitely-missing");

    response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

    using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    problem.RootElement.GetProperty("errorCode").GetString().Should().Be("ResourceNotFound");
    problem.RootElement.GetProperty("status").GetInt32().Should().Be(404);
    problem.RootElement.GetProperty("instance").GetString().Should().Be("/api/v1/definitely-missing");
  }

  [Fact]
  public async Task should_serve_openapi_document_when_environment_is_development()
  {
    using var factory = CreateFactory("Development");
    using var client = factory.CreateClient();

    using var response = await client.GetAsync("/openapi/v1.json");

    response.IsSuccessStatusCode.Should().BeTrue();
  }

  [Fact]
  public async Task should_serve_scalar_reference_when_environment_is_development()
  {
    using var factory = CreateFactory("Development");
    using var client = factory.CreateClient();

    using var response = await client.GetAsync("/scalar/v1");

    response.IsSuccessStatusCode.Should().BeTrue();
  }

  [Fact]
  public async Task should_not_serve_scalar_reference_when_environment_is_production()
  {
    using var factory = CreateFactory("Production");
    using var client = factory.CreateClient();

    using var response = await client.GetAsync("/scalar/v1");

    response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
  }
}
