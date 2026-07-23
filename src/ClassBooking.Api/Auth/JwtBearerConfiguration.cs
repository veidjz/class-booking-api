using ClassBooking.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ClassBooking.Api.Auth;

internal sealed class JwtBearerConfiguration : IConfigureNamedOptions<JwtBearerOptions>
{
  private static readonly TimeSpan LifetimeTolerance = TimeSpan.FromSeconds(30);

  private readonly JwtOptions _jwtOptions;
  private readonly TimeProvider _clock;

  public JwtBearerConfiguration(IOptions<JwtOptions> jwtOptions, TimeProvider clock)
  {
    _jwtOptions = jwtOptions.Value;
    _clock = clock;
  }

  public void Configure(string? name, JwtBearerOptions options)
  {
    if (name == JwtBearerDefaults.AuthenticationScheme)
    {
      Configure(options);
    }
  }

  public void Configure(JwtBearerOptions options)
  {
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
      ValidateIssuer = true,
      ValidateAudience = true,
      ValidateLifetime = true,
      ValidateIssuerSigningKey = true,
      ValidIssuer = _jwtOptions.Issuer,
      ValidAudience = _jwtOptions.Audience,
      IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(_jwtOptions.SigningKey)),
      RoleClaimType = AuthorizationPolicies.RoleClaim,
      NameClaimType = "sub",
      // Replaces the built-in check, which reads the machine clock, so expiration follows the injected TimeProvider.
      LifetimeValidator = (_, expires, _, _) =>
          expires is not null
          && _clock.GetUtcNow() <= new DateTimeOffset(expires.Value, TimeSpan.Zero).Add(LifetimeTolerance),
    };
  }
}
