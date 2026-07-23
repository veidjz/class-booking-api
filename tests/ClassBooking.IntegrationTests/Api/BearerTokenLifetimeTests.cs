using ClassBooking.Application.Abstractions.Auth;
using ClassBooking.Domain.Users;
using ClassBooking.IntegrationTests.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ClassBooking.IntegrationTests.Api;

public sealed class BearerTokenLifetimeTests : IDisposable
{
  private static readonly DateTimeOffset IssuedAt = new DateTimeOffset(2026, 3, 2, 12, 0, 0, TimeSpan.Zero);

  private readonly FakeTimeProvider _clock = new FakeTimeProvider(IssuedAt);
  private readonly WebApplicationFactory<Program> _root = new WebApplicationFactory<Program>();
  private readonly WebApplicationFactory<Program> _factory;

  public BearerTokenLifetimeTests() =>
      _factory = _root.Configure(
          ApiHost.UnusedConnectionString,
          configureServices: services => services.Replace(ServiceDescriptor.Singleton<TimeProvider>(_clock)));

  public void Dispose() => _root.Dispose();

  [Theory]
  [InlineData(0, true)]
  [InlineData(59 * 60, true)]
  [InlineData(60 * 60 + 29, true)]
  [InlineData(60 * 60 + 31, false)]
  [InlineData(61 * 60, false)]
  public async Task should_validate_the_token_lifetime_on_the_injected_clock(int secondsAfterIssuance, bool valid)
  {
    ITokenService tokenService = _factory.Services.GetRequiredService<ITokenService>();
    Student student = Student.Register("Ana Souza", "ana.souza@example.com", "hash", IssuedAt);
    AccessToken accessToken = tokenService.Issue(student);

    _clock.Advance(TimeSpan.FromSeconds(secondsAfterIssuance));

    TokenValidationResult result = await new JsonWebTokenHandler()
        .ValidateTokenAsync(accessToken.Token, BearerValidationParameters());

    result.IsValid.Should().Be(valid);
  }

  private TokenValidationParameters BearerValidationParameters() =>
      _factory.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
          .Get(JwtBearerDefaults.AuthenticationScheme)
          .TokenValidationParameters;
}
