using ClassBooking.Application.Abstractions.Auth;
using ClassBooking.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ClassBooking.Infrastructure.Auth;

internal sealed class JwtTokenService : ITokenService
{
  private readonly JwtOptions _options;
  private readonly TimeProvider _clock;
  private readonly SigningCredentials _signingCredentials;

  public JwtTokenService(IOptions<JwtOptions> options, TimeProvider clock)
  {
    _options = options.Value;
    _clock = clock;
    _signingCredentials = new SigningCredentials(
        new SymmetricSecurityKey(Convert.FromBase64String(_options.SigningKey)),
        SecurityAlgorithms.HmacSha256);
  }

  public AccessToken Issue(User user)
  {
    DateTimeOffset now = _clock.GetUtcNow();
    JsonWebTokenHandler handler = new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = false };

    string token = handler.CreateToken(new SecurityTokenDescriptor
    {
      SigningCredentials = _signingCredentials,
      Claims = new Dictionary<string, object>
      {
        ["iss"] = _options.Issuer,
        ["aud"] = _options.Audience,
        ["sub"] = user.Id.ToString("D"),
        ["role"] = user.Role.ToString(),
        ["jti"] = Guid.CreateVersion7(now).ToString("D"),
        ["iat"] = now.ToUnixTimeSeconds(),
        ["exp"] = now.Add(AuthConstants.AccessTokenLifetime).ToUnixTimeSeconds(),
      },
    });

    return new AccessToken(token, (int)AuthConstants.AccessTokenLifetime.TotalSeconds);
  }
}
