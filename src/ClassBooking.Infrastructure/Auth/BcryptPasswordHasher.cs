using ClassBooking.Application.Abstractions.Auth;

namespace ClassBooking.Infrastructure.Auth;

internal sealed class BcryptPasswordHasher : IPasswordHasher
{
  public string Hash(string password) =>
      BCrypt.Net.BCrypt.EnhancedHashPassword(password, AuthConstants.BcryptWorkFactor);

  /// <remarks>
  /// A stored hash that cannot be parsed is a rejected credential, not a fault: the caller is
  /// authenticating, and an exception here would turn a wrong password into a server error.
  /// </remarks>
  public bool Verify(string password, string hash)
  {
    try
    {
      return BCrypt.Net.BCrypt.EnhancedVerify(password, hash);
    }
    catch (Exception exception) when (exception is BCrypt.Net.SaltParseException or ArgumentException)
    {
      return false;
    }
  }
}
