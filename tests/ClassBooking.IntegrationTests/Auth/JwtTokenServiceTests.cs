using System.Text.Json;
using ClassBooking.Application.Abstractions.Auth;
using ClassBooking.Domain.Users;
using ClassBooking.Infrastructure.Auth;
using ClassBooking.IntegrationTests.Support;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ClassBooking.IntegrationTests.Auth;

public sealed class JwtTokenServiceTests
{
  private static readonly DateTimeOffset IssuedAt = new DateTimeOffset(2026, 3, 2, 12, 0, 0, TimeSpan.Zero);

  private readonly ITokenService _tokenService;
  private readonly Student _student;

  public JwtTokenServiceTests()
  {
    JwtOptions options = new JwtOptions { SigningKey = ApiHost.TestSigningKey };
    _tokenService = new JwtTokenService(Options.Create(options), new FakeTimeProvider(IssuedAt));
    _student = Student.Register("Ana Souza", "ana.souza@example.com", "hash", IssuedAt);
  }

  [Fact]
  public void should_issue_exactly_the_closed_claim_set()
  {
    AccessToken accessToken = _tokenService.Issue(_student);

    using JsonDocument payload = DecodeSegment(accessToken.Token, segment: 1);

    payload.RootElement.EnumerateObject().Select(property => property.Name)
        .Should().BeEquivalentTo("iss", "aud", "sub", "role", "jti", "iat", "exp");
  }

  [Fact]
  public void should_sign_with_hs256()
  {
    AccessToken accessToken = _tokenService.Issue(_student);

    using JsonDocument header = DecodeSegment(accessToken.Token, segment: 0);

    header.RootElement.GetProperty("alg").GetString().Should().Be("HS256");
  }

  [Fact]
  public void should_identify_the_user_and_role()
  {
    AccessToken accessToken = _tokenService.Issue(_student);

    using JsonDocument payload = DecodeSegment(accessToken.Token, segment: 1);

    payload.RootElement.GetProperty("iss").GetString().Should().Be("classbooking-api");
    payload.RootElement.GetProperty("aud").GetString().Should().Be("classbooking");
    payload.RootElement.GetProperty("sub").GetString().Should().Be(_student.Id.ToString("D"));
    payload.RootElement.GetProperty("role").GetString().Should().Be("Student");
  }

  [Fact]
  public void should_stamp_issuance_and_expiration_from_the_injected_clock()
  {
    AccessToken accessToken = _tokenService.Issue(_student);

    using JsonDocument payload = DecodeSegment(accessToken.Token, segment: 1);

    payload.RootElement.GetProperty("iat").GetInt64().Should().Be(IssuedAt.ToUnixTimeSeconds());
    payload.RootElement.GetProperty("exp").GetInt64().Should().Be(IssuedAt.AddMinutes(60).ToUnixTimeSeconds());
    accessToken.ExpiresInSeconds.Should().Be(3600);
  }

  [Fact]
  public void should_stamp_a_distinct_time_ordered_token_id_per_issuance()
  {
    AccessToken first = _tokenService.Issue(_student);
    AccessToken second = _tokenService.Issue(_student);

    Guid firstId = TokenId(first);
    Guid secondId = TokenId(second);

    secondId.Should().NotBe(firstId);
    firstId.Version.Should().Be(7);
    Timestamp(firstId).Should().Be(IssuedAt.ToUnixTimeMilliseconds());
  }

  [Fact]
  public async Task should_sign_with_the_configured_key()
  {
    AccessToken accessToken = _tokenService.Issue(_student);

    TokenValidationResult withConfiguredKey = await ValidateSignatureAsync(accessToken.Token, ApiHost.TestSigningKey);
    TokenValidationResult withAnotherKey = await ValidateSignatureAsync(accessToken.Token, Convert.ToBase64String(new byte[64]));

    withConfiguredKey.IsValid.Should().BeTrue();
    withAnotherKey.IsValid.Should().BeFalse();
  }

  private static Task<TokenValidationResult> ValidateSignatureAsync(string token, string signingKey) =>
      new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
      {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = false,
        IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(signingKey)),
      });

  private static Guid TokenId(AccessToken accessToken)
  {
    using JsonDocument payload = DecodeSegment(accessToken.Token, segment: 1);

    return Guid.Parse(payload.RootElement.GetProperty("jti").GetString()!);
  }

  private static JsonDocument DecodeSegment(string token, int segment) =>
      JsonDocument.Parse(Base64UrlEncoder.Decode(token.Split('.')[segment]));

  private static long Timestamp(Guid id)
  {
    byte[] bytes = id.ToByteArray(bigEndian: true);

    return ((long)bytes[0] << 40)
        | ((long)bytes[1] << 32)
        | ((long)bytes[2] << 24)
        | ((long)bytes[3] << 16)
        | ((long)bytes[4] << 8)
        | bytes[5];
  }
}
