using ClassBooking.IntegrationTests.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ClassBooking.IntegrationTests.Api;

public sealed class AuthCompositionTests : IDisposable
{
  private readonly WebApplicationFactory<Program> _root = new WebApplicationFactory<Program>();
  private readonly WebApplicationFactory<Program> _factory;

  public AuthCompositionTests() => _factory = _root.Configure(ApiHost.UnusedConnectionString);

  public void Dispose() => _root.Dispose();

  [Fact]
  public void should_require_an_authenticated_user_by_default()
  {
    AuthorizationOptions options = AuthorizationOptions();

    options.FallbackPolicy.Should().NotBeNull();
    options.FallbackPolicy!.Requirements
        .Should().ContainSingle(requirement => requirement is DenyAnonymousAuthorizationRequirement);
  }

  [Theory]
  [InlineData("StudentOnly", new[] { "Student" })]
  [InlineData("TeacherOnly", new[] { "Teacher" })]
  [InlineData("AdminOnly", new[] { "Admin" })]
  [InlineData("StudentOrAdmin", new[] { "Student", "Admin" })]
  public void should_gate_each_role_policy_on_the_role_claim(string policyName, string[] allowedRoles)
  {
    AuthorizationPolicy? policy = AuthorizationOptions().GetPolicy(policyName);

    policy.Should().NotBeNull();
    ClaimsAuthorizationRequirement requirement =
        policy!.Requirements.OfType<ClaimsAuthorizationRequirement>().Single();
    requirement.ClaimType.Should().Be("role");
    requirement.AllowedValues.Should().BeEquivalentTo(allowedRoles);
  }

  [Fact]
  public void should_authenticate_with_the_bearer_scheme_by_default()
  {
    AuthenticationOptions options =
        _factory.Services.GetRequiredService<IOptions<AuthenticationOptions>>().Value;

    options.DefaultScheme.Should().Be(JwtBearerDefaults.AuthenticationScheme);
  }

  [Fact]
  public void should_validate_bearer_tokens_against_the_configured_issuer_audience_and_key()
  {
    JwtBearerOptions options = BearerOptions();

    TokenValidationParameters parameters = options.TokenValidationParameters;
    parameters.ValidateIssuer.Should().BeTrue();
    parameters.ValidateAudience.Should().BeTrue();
    parameters.ValidateLifetime.Should().BeTrue();
    parameters.ValidateIssuerSigningKey.Should().BeTrue();
    parameters.ValidIssuer.Should().Be("classbooking-api");
    parameters.ValidAudience.Should().Be("classbooking");
    parameters.IssuerSigningKey.Should().BeOfType<SymmetricSecurityKey>()
        .Which.Key.Should().Equal(Convert.FromBase64String(ApiHost.TestSigningKey));
  }

  [Fact]
  public void should_read_the_token_claims_without_inbound_mapping()
  {
    JwtBearerOptions options = BearerOptions();

    options.MapInboundClaims.Should().BeFalse();
    options.TokenValidationParameters.RoleClaimType.Should().Be("role");
    options.TokenValidationParameters.NameClaimType.Should().Be("sub");
  }

  [Fact]
  public async Task should_challenge_a_real_endpoint_that_fails_authorization()
  {
    using IServiceScope scope = _factory.Services.CreateScope();
    HttpContext context = HttpContextWith(scope, new Endpoint(
        _ => Task.CompletedTask,
        new EndpointMetadataCollection(new HttpMethodMetadata(["GET"])),
        "real endpoint"));
    bool passedThrough = false;

    await ResultHandler().HandleAsync(
        _ => { passedThrough = true; return Task.CompletedTask; },
        context,
        FallbackPolicy(),
        PolicyAuthorizationResult.Challenge());

    passedThrough.Should().BeFalse();
    context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
  }

  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public async Task should_let_a_routing_rejection_keep_its_transport_status(bool bareRejectionEndpoint)
  {
    using IServiceScope scope = _factory.Services.CreateScope();
    HttpContext context = HttpContextWith(scope, bareRejectionEndpoint
        ? new Endpoint(_ => Task.CompletedTask, EndpointMetadataCollection.Empty, "405 HTTP Method Not Allowed")
        : null);
    bool passedThrough = false;

    await ResultHandler().HandleAsync(
        _ => { passedThrough = true; return Task.CompletedTask; },
        context,
        FallbackPolicy(),
        PolicyAuthorizationResult.Challenge());

    passedThrough.Should().BeTrue();
    context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
  }

  private static HttpContext HttpContextWith(IServiceScope scope, Endpoint? endpoint)
  {
    DefaultHttpContext context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
    context.SetEndpoint(endpoint);

    return context;
  }

  private IAuthorizationMiddlewareResultHandler ResultHandler() =>
      _factory.Services.GetRequiredService<IAuthorizationMiddlewareResultHandler>();

  private AuthorizationPolicy FallbackPolicy() =>
      AuthorizationOptions().FallbackPolicy!;

  private AuthorizationOptions AuthorizationOptions() =>
      _factory.Services.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

  private JwtBearerOptions BearerOptions() =>
      _factory.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
          .Get(JwtBearerDefaults.AuthenticationScheme);
}
