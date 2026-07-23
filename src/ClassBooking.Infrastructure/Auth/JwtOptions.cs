namespace ClassBooking.Infrastructure.Auth;

public sealed class JwtOptions
{
  public const string SectionName = "Jwt";

  public string SigningKey { get; set; } = string.Empty;

  public string Issuer { get; set; } = "classbooking-api";

  public string Audience { get; set; } = "classbooking";

  public bool HasValidSigningKey()
  {
    byte[] buffer = new byte[SigningKey.Length];
    return Convert.TryFromBase64String(SigningKey, buffer, out int decodedByteCount) && decodedByteCount >= 32;
  }
}
