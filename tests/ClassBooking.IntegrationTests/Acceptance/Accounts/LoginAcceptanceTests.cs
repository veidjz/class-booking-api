using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClassBooking.Application.Abstractions.Auth;
using ClassBooking.Domain.Users;
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

  /// <summary>An enhanced hash of <see cref="Password" /> at work factor 11, one below the current 12.</summary>
  private const string StaleHash = "$2a$11$tZum8Gn4/lfAsLKs98IPUu.7BVfWFx3yixd06l206NiKzp5T38T0O";

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
    response.Headers.CacheControl!.NoStore.Should().BeTrue("the response carries a token");

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

  [Fact]
  [Trait("Scenario", "ACC-ADM-09")]
  public async Task should_reject_bad_logins_with_one_indistinguishable_error()
  {
    using HttpClient client = _factory.CreateClient();
    await RegisterAsync(client);
    await SeedDeactivatedAsync("paulo@classbooking.dev");

    string wrongPassword = await UnauthorizedBodyAsync(client, Email, "senha-errada-1");
    string unknownEmail = await UnauthorizedBodyAsync(client, "ghost@classbooking.dev", Password);
    string deactivatedAccount = await UnauthorizedBodyAsync(client, "paulo@classbooking.dev", Password);

    WithoutTraceId(wrongPassword).Should().Be(WithoutTraceId(unknownEmail));
    WithoutTraceId(unknownEmail).Should().Be(WithoutTraceId(deactivatedAccount));
  }

  [Fact]
  [Trait("Scenario", "ACC-ADM-09")]
  public async Task should_answer_a_short_password_with_the_uniform_401_never_a_400()
  {
    using HttpClient client = _factory.CreateClient();
    await RegisterAsync(client);

    using HttpResponseMessage response = await client.PostAsJsonAsync(
        LoginRoute,
        new { email = Email, password = "seven77" });

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  [Fact]
  [Trait("Scenario", "ACC-ADM-09")]
  public async Task should_not_touch_the_stored_hash_when_the_login_fails()
  {
    await AddAsync(Student.Register("Ana Souza", Email, StaleHash, Now));
    using HttpClient client = _factory.CreateClient();

    using HttpResponseMessage response = await client.PostAsJsonAsync(
        LoginRoute,
        new { email = Email, password = "senha-errada-1" });

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    (await ScalarAsync<string>("select password_hash from users")).Should().Be(StaleHash);
  }

  [Fact]
  public async Task should_persist_a_stronger_hash_when_the_stored_one_is_stale()
  {
    await AddAsync(Student.Register("Ana Souza", Email, StaleHash, Now));
    using HttpClient client = _factory.CreateClient();

    using HttpResponseMessage first = await client.PostAsJsonAsync(
        LoginRoute,
        new { email = Email, password = Password });

    first.StatusCode.Should().Be(HttpStatusCode.OK);
    string upgraded = (await ScalarAsync<string>("select password_hash from users"))!;
    upgraded.Should().StartWith("$2a$12$").And.NotBe(StaleHash);
    _factory.Services.GetRequiredService<IPasswordHasher>().Verify(Password, upgraded).Should().BeTrue();

    using HttpResponseMessage second = await client.PostAsJsonAsync(
        LoginRoute,
        new { email = Email, password = Password });

    second.StatusCode.Should().Be(HttpStatusCode.OK);
    (await ScalarAsync<string>("select password_hash from users")).Should().Be(upgraded);
  }

  [Fact]
  public async Task should_reject_a_missing_field_as_a_validation_failure()
  {
    using HttpClient client = _factory.CreateClient();

    using HttpResponseMessage response = await client.PostAsJsonAsync(LoginRoute, new { });

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

    using JsonDocument problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    problem.RootElement.GetProperty("errorCode").GetString().Should().Be("ValidationFailed");
    problem.RootElement.GetProperty("instance").GetString().Should().Be(LoginRoute);
    problem.RootElement.GetProperty("errors").EnumerateObject()
        .Select(property => property.Name)
        .Should().BeEquivalentTo("email", "password");
  }

  private async Task SeedDeactivatedAsync(string email)
  {
    string passwordHash = _factory.Services.GetRequiredService<IPasswordHasher>().Hash(Password);
    Student student = Student.Register("Paulo Lima", email, passwordHash, Now);
    student.Deactivate(Now);
    await AddAsync(student);
  }

  private async Task<string> UnauthorizedBodyAsync(HttpClient client, string email, string password)
  {
    using HttpResponseMessage response = await client.PostAsJsonAsync(
        LoginRoute,
        new { email, password });

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

    string body = await response.Content.ReadAsStringAsync();
    using JsonDocument problem = JsonDocument.Parse(body);
    problem.RootElement.GetProperty("errorCode").GetString().Should().Be("InvalidCredentials");
    problem.RootElement.GetProperty("instance").GetString().Should().Be(LoginRoute);

    return body;
  }

  private static string WithoutTraceId(string problemJson) =>
      Regex.Replace(problemJson, "\"traceId\":\"[^\"]+\"", "\"traceId\":\"<masked>\"");

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
