using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ClassBooking.IntegrationTests.Api;

public sealed class TransportErrorTests
{
  private const string ConnectionString =
      "Server=localhost;Port=3306;Database=classbooking;User Id=classbooking;Password=classbooking";

  private const string Route = "/api/v1/auth/register";

  private static WebApplicationFactory<Program> CreateFactory(string environment) =>
      new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
      {
        builder.UseEnvironment(environment);
        builder.UseSetting("ConnectionStrings:Database", ConnectionString);
      });

  [Theory]
  [InlineData("Development")]
  [InlineData("Production")]
  public async Task should_return_validation_failed_problem_when_the_body_is_not_json(string environment)
  {
    using WebApplicationFactory<Program> factory = CreateFactory(environment);
    using HttpClient client = factory.CreateClient();

    using StringContent content = new StringContent("{not json", Encoding.UTF8, "application/json");
    using HttpResponseMessage response = await client.PostAsync(Route, content);

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

    using JsonDocument problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    problem.RootElement.GetProperty("errorCode").GetString().Should().Be("ValidationFailed");
    problem.RootElement.GetProperty("status").GetInt32().Should().Be(400);
    problem.RootElement.GetProperty("instance").GetString().Should().Be(Route);
  }

  [Fact]
  public async Task should_return_unsupported_media_type_when_the_content_type_is_not_json()
  {
    using WebApplicationFactory<Program> factory = CreateFactory("Production");
    using HttpClient client = factory.CreateClient();

    using StringContent content = new StringContent("ana", Encoding.UTF8, "text/plain");
    using HttpResponseMessage response = await client.PostAsync(Route, content);

    response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
  }

  [Fact]
  public async Task should_return_method_not_allowed_when_the_verb_is_wrong()
  {
    using WebApplicationFactory<Program> factory = CreateFactory("Production");
    using HttpClient client = factory.CreateClient();

    using HttpResponseMessage response = await client.GetAsync(Route);

    response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    response.Content.Headers.Allow.Should().Contain("POST");
  }

  [Fact]
  public async Task should_return_resource_not_found_problem_when_the_route_is_unknown()
  {
    using WebApplicationFactory<Program> factory = CreateFactory("Production");
    using HttpClient client = factory.CreateClient();

    using HttpResponseMessage response = await client.GetAsync("/api/v1/auth/refresh");

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

    using JsonDocument problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    problem.RootElement.GetProperty("errorCode").GetString().Should().Be("ResourceNotFound");
  }
}
