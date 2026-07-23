using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClassBooking.IntegrationTests.Persistence.Fixtures;
using ClassBooking.IntegrationTests.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.Tokens;

namespace ClassBooking.IntegrationTests.Acceptance.Accounts;

[Collection(nameof(DatabaseCollection))]
public sealed class LoginAcceptanceTests : DatabaseTestBase, IDisposable
{
  private const string LoginRoute = "/api/v1/auth/login";
  private const string RegisterRoute = "/api/v1/auth/register";
  private const string Email = "ana.souza@example.com";
  private const string Password = "s3nh4-segura";

  private static readonly DateTimeOffset Now = new DateTimeOffset(2026, 3, 2, 12, 0, 0, TimeSpan.Zero);

  private readonly FakeTimeProvider _clock = new FakeTimeProvider(Now);
  private readonly WebApplicationFactory<Program> _root = new WebApplicationFactory<Program>();
  private readonly WebApplicationFactory<Program> _factory;

  public LoginAcceptanceTests(ContainersFixture fixture)
      : base(fixture) =>
      _factory = _root.Configure(
          fixture.ConnectionString,
          configureServices: services => services.Replace(ServiceDescriptor.Singleton<TimeProvider>(_clock)));

  public void Dispose() => _root.Dispose();

  [Fact]
  [Trait("Scenario", "ACC-ADM-08")]
  public async Task should_issue_a_bearer_token_when_the_credentials_are_valid()
  {
    using HttpClient client = _factory.CreateClient();
    string userId = await RegisterAsync(client);

    using HttpResponseMessage response = await client.PostAsJsonAsync(
        LoginRoute,
        new { email = Email, password = Password });

    response.StatusCode.Should().Be(HttpStatusCode.OK);

    using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    body.RootElement.EnumerateObject().Select(property => property.Name)
        .Should().BeEquivalentTo("tokenType", "accessToken", "expiresIn");
    body.RootElement.GetProperty("tokenType").GetString().Should().Be("Bearer");
    body.RootElement.GetProperty("expiresIn").GetInt32().Should().Be(3600);

    using JsonDocument payload = DecodePayload(body.RootElement.GetProperty("accessToken").GetString()!);
    payload.RootElement.EnumerateObject().Select(property => property.Name)
        .Should().BeEquivalentTo("iss", "aud", "sub", "role", "jti", "iat", "exp");
    payload.RootElement.GetProperty("iss").GetString().Should().Be("classbooking-api");
    payload.RootElement.GetProperty("aud").GetString().Should().Be("classbooking");
    payload.RootElement.GetProperty("sub").GetString().Should().Be(userId);
    payload.RootElement.GetProperty("role").GetString().Should().Be("Student");
    Guid.Parse(payload.RootElement.GetProperty("jti").GetString()!).Version.Should().Be(7);
    payload.RootElement.GetProperty("iat").GetInt64().Should().Be(Now.ToUnixTimeSeconds());
    payload.RootElement.GetProperty("exp").GetInt64().Should().Be(Now.AddMinutes(60).ToUnixTimeSeconds());
  }

  private async Task<string> RegisterAsync(HttpClient client)
  {
    using HttpResponseMessage response = await client.PostAsJsonAsync(
        RegisterRoute,
        new { name = "Ana Souza", email = Email, password = Password });

    response.StatusCode.Should().Be(HttpStatusCode.Created);

    using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

    return body.RootElement.GetProperty("id").GetString()!;
  }

  private static JsonDocument DecodePayload(string token) =>
      JsonDocument.Parse(Base64UrlEncoder.Decode(token.Split('.')[1]));
}
