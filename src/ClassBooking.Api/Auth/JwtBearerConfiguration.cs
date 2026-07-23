using ClassBooking.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ClassBooking.Api.Auth;

internal sealed class JwtBearerConfiguration : IConfigureNamedOptions<JwtBearerOptions>
{
  private readonly JwtOptions _jwtOptions;

  public JwtBearerConfiguration(IOptions<JwtOptions> jwtOptions) => _jwtOptions = jwtOptions.Value;

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
    };
  }
}
