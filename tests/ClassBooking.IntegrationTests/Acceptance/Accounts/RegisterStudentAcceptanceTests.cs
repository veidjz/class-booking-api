using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClassBooking.Api.Endpoints.Auth;
using ClassBooking.Application.Abstractions.Auth;
using ClassBooking.Domain.Users;
using ClassBooking.IntegrationTests.Persistence.Fixtures;
using ClassBooking.IntegrationTests.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace ClassBooking.IntegrationTests.Acceptance.Accounts;

[Collection(nameof(DatabaseCollection))]
public sealed class RegisterStudentAcceptanceTests : DatabaseTestBase, IDisposable
{
  private const string Route = "/api/v1/auth/register";
  private const string Password = "s3nh4-segura";

  private static readonly DateTimeOffset Now = new DateTimeOffset(2026, 3, 2, 12, 0, 0, TimeSpan.Zero);

  private readonly FakeTimeProvider _clock = new FakeTimeProvider(Now);
  private readonly WebApplicationFactory<Program> _root = new WebApplicationFactory<Program>();
  private readonly WebApplicationFactory<Program> _factory;

  public RegisterStudentAcceptanceTests(ContainersFixture fixture)
      : base(fixture) =>
      _factory = _root.Configure(
          fixture.ConnectionString,
          configureServices: services => services.Replace(ServiceDescriptor.Singleton<TimeProvider>(_clock)));

  public void Dispose() => _root.Dispose();

  [Fact]
  [Trait("Scenario", "ACC-ADM-05")]
  public async Task should_create_an_active_student_account_when_a_visitor_registers()
  {
    using HttpClient client = _factory.CreateClient();

    using HttpResponseMessage response = await client.PostAsJsonAsync(
        Route,
        new { name = "  Ana Souza  ", email = "  ANA.Souza@Example.COM  ", password = Password });

    response.StatusCode.Should().Be(HttpStatusCode.Created);

    using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    string id = body.RootElement.GetProperty("id").GetString()!;
    body.RootElement.GetProperty("name").GetString().Should().Be("Ana Souza");
    body.RootElement.GetProperty("email").GetString().Should().Be("ana.souza@example.com");
    body.RootElement.GetProperty("active").GetBoolean().Should().BeTrue();
    response.Headers.Location!.ToString().Should().Be($"/api/v1/students/{id}");

    IReadOnlyList<(string Name, string Email, string Role, bool IsActive)> rows = await QueryAsync(
        "select name, email, role, is_active from users",
        reader => (reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetBoolean(3)));

    rows.Should().ContainSingle();
    rows[0].Should().Be(("Ana Souza", "ana.souza@example.com", "Student", true));
    (await ScalarAsync<long>("select count(*) from students")).Should().Be(1);
    (await ScalarAsync<long>("select count(*) from teachers")).Should().Be(0);
  }

  [Fact]
  [Trait("Scenario", "ACC-ADM-05")]
  public async Task should_let_the_fresh_student_authenticate()
  {
    using JsonDocument registered = await RegisterAsync();
    using HttpClient client = _factory.CreateClient();

    using HttpResponseMessage response = await client.PostAsJsonAsync(
        "/api/v1/auth/login",
        new { email = "ana.souza@example.com", password = Password });

    response.StatusCode.Should().Be(HttpStatusCode.OK);

    using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    body.RootElement.GetProperty("tokenType").GetString().Should().Be("Bearer");
    body.RootElement.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
  }

  [Fact]
  public async Task should_store_a_hash_that_verifies_against_the_submitted_password()
  {
    using JsonDocument registered = await RegisterAsync();

    string hash = (await ScalarAsync<string>("select password_hash from users"))!;

    hash.Should().NotBe(Password);
    IPasswordHasher hasher = _factory.Services.GetRequiredService<IPasswordHasher>();
    hasher.Verify(Password, hash).Should().BeTrue();
  }

  [Fact]
  public async Task should_serialize_the_account_role_as_the_domain_name()
  {
    using JsonDocument body = await RegisterAsync();

    body.RootElement.GetProperty("role").GetString().Should().Be("Student");
  }

  [Fact]
  public async Task should_serialize_the_creation_instant_as_utc_with_a_z_suffix()
  {
    using JsonDocument body = await RegisterAsync();

    body.RootElement.GetProperty("createdAt").GetString().Should().Be("2026-03-02T12:00:00Z");
  }

  [Fact]
  [Trait("Scenario", "ACC-ADM-07")]
  public async Task should_reject_the_registration_when_the_email_belongs_to_an_account()
  {
    await AddAsync(Student.Register("Ana", "ana@classbooking.dev", "hash", Now));

    await AssertConflictAsync();
  }

  [Fact]
  public async Task should_reject_the_registration_when_the_email_belongs_to_a_deactivated_account()
  {
    Student student = Student.Register("Ana", "ana@classbooking.dev", "hash", Now);
    student.Deactivate(Now);
    await AddAsync(student);

    await AssertConflictAsync();

    (await ScalarAsync<bool>("select is_active from users")).Should().BeFalse();
  }

  [Fact]
  public async Task should_reject_the_registration_when_the_email_belongs_to_a_teacher()
  {
    await AddAsync(Teacher.Create("Paulo", "ana@classbooking.dev", "hash", Now));

    await AssertConflictAsync();
  }

  /// <remarks>
  /// The behaviour below only holds because the request carries no role to bind: a member added
  /// here would be bound and the payload would start deciding the role. This pins the shape.
  /// </remarks>
  [Fact]
  [Trait("Scenario", "ACC-ADM-06")]
  public void should_not_offer_a_role_to_the_payload()
  {
    typeof(RegisterStudentRequest).GetProperties().Select(property => property.Name)
        .Should().BeEquivalentTo("Name", "Email", "Password");
  }

  [Fact]
  [Trait("Scenario", "ACC-ADM-06")]
  public async Task should_create_a_student_when_the_payload_carries_a_role()
  {
    using HttpClient client = _factory.CreateClient();

    using HttpResponseMessage response = await client.PostAsJsonAsync(
        Route,
        new { name = "Ana Souza", email = "ana.souza@example.com", password = Password, role = "Teacher" });

    response.StatusCode.Should().Be(HttpStatusCode.Created);

    using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    body.RootElement.GetProperty("role").GetString().Should().Be("Student");

    (await ScalarAsync<string>("select role from users")).Should().Be("Student");
    (await ScalarAsync<long>("select count(*) from teachers")).Should().Be(0);
  }

  [Fact]
  public async Task should_reject_the_registration_when_the_payload_is_malformed()
  {
    await AssertValidationFailedAsync(new { name = "   ", email = "not-an-email", password = "abc" });
  }

  [Fact]
  public async Task should_reject_the_registration_when_the_body_is_an_empty_object()
  {
    await AssertValidationFailedAsync(new { });
  }

  private async Task AssertValidationFailedAsync(object payload)
  {
    using HttpClient client = _factory.CreateClient();

    using HttpResponseMessage response = await client.PostAsJsonAsync(Route, payload);

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

    using JsonDocument problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    problem.RootElement.GetProperty("errorCode").GetString().Should().Be("ValidationFailed");
    problem.RootElement.GetProperty("instance").GetString().Should().Be(Route);
    problem.RootElement.GetProperty("errors").EnumerateObject()
        .Select(property => property.Name)
        .Should().BeEquivalentTo("name", "email", "password");

    (await ScalarAsync<long>("select count(*) from users")).Should().Be(0);
  }

  private async Task AssertConflictAsync()
  {
    using HttpClient client = _factory.CreateClient();

    using HttpResponseMessage response = await client.PostAsJsonAsync(
        Route,
        new { name = "Ana Souza", email = "  ANA@ClassBooking.dev  ", password = Password });

    response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

    using JsonDocument problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    problem.RootElement.GetProperty("errorCode").GetString().Should().Be("EmailAlreadyInUse");
    problem.RootElement.GetProperty("instance").GetString().Should().Be(Route);

    (await ScalarAsync<long>("select count(*) from users")).Should().Be(1);
  }

  private async Task<JsonDocument> RegisterAsync()
  {
    using HttpClient client = _factory.CreateClient();

    using HttpResponseMessage response = await client.PostAsJsonAsync(
        Route,
        new { name = "Ana Souza", email = "ana.souza@example.com", password = Password });

    response.StatusCode.Should().Be(HttpStatusCode.Created);

    return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
  }
}
