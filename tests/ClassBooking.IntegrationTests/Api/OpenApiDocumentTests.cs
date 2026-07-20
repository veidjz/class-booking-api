using System.Text.Json;
using ClassBooking.IntegrationTests.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ClassBooking.IntegrationTests.Api;

public sealed class OpenApiDocumentTests : IAsyncLifetime
{
  private readonly WebApplicationFactory<Program> _root = new WebApplicationFactory<Program>();
  private JsonDocument _document = null!;

  public async Task InitializeAsync()
  {
    using WebApplicationFactory<Program> factory =
        _root.Configure(ApiHost.UnusedConnectionString, "Development");
    using HttpClient client = factory.CreateClient();

    _document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
  }

  public Task DisposeAsync()
  {
    _document.Dispose();
    _root.Dispose();

    return Task.CompletedTask;
  }

  [Fact]
  public void should_publish_the_type_of_an_instant()
  {
    Schema("RegisterStudentResponse").GetProperty("properties").GetProperty("createdAt")
        .GetProperty("type").GetString().Should().Be("string");
  }

  [Fact]
  public void should_publish_the_type_of_an_enum()
  {
    JsonElement role = Schema("UserRole");

    role.GetProperty("type").GetString().Should().Be("string");
    role.GetProperty("enum").EnumerateArray().Select(value => value.GetString())
        .Should().BeEquivalentTo("Student", "Teacher", "Admin");
  }

  [Theory]
  [InlineData("400")]
  [InlineData("409")]
  [InlineData("429")]
  public void should_publish_the_members_that_carry_the_error_contract(string status)
  {
    JsonElement properties = ErrorSchema(status).GetProperty("properties");

    properties.TryGetProperty("errorCode", out _).Should().BeTrue();
    properties.TryGetProperty("traceId", out _).Should().BeTrue();
  }

  [Fact]
  public void should_publish_the_field_map_only_on_the_validation_response()
  {
    ErrorSchema("400").GetProperty("properties").TryGetProperty("errors", out _).Should().BeTrue();
    ErrorSchema("409").GetProperty("properties").TryGetProperty("errors", out _).Should().BeFalse();
  }

  [Fact]
  public void should_publish_the_location_header_of_the_created_account()
  {
    Operation().GetProperty("responses").GetProperty("201").GetProperty("headers")
        .GetProperty("Location").GetProperty("schema").GetProperty("type").GetString().Should().Be("string");
  }

  [Fact]
  public void should_publish_a_stable_operation_name_and_tag()
  {
    Operation().GetProperty("operationId").GetString().Should().Be("RegisterStudent");
    Operation().GetProperty("tags").EnumerateArray().Select(tag => tag.GetString())
        .Should().BeEquivalentTo("Auth");
  }

  private JsonElement Operation() =>
      _document.RootElement.GetProperty("paths").GetProperty("/api/v1/auth/register").GetProperty("post");

  private JsonElement ErrorSchema(string status) =>
      Resolve(Operation().GetProperty("responses").GetProperty(status)
          .GetProperty("content").GetProperty("application/problem+json").GetProperty("schema"));

  private JsonElement Schema(string name) =>
      _document.RootElement.GetProperty("components").GetProperty("schemas").GetProperty(name);

  private JsonElement Resolve(JsonElement schema) =>
      schema.TryGetProperty("$ref", out JsonElement reference)
          ? Schema(reference.GetString()!.Split('/')[^1])
          : schema;
}
