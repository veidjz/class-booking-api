using ClassBooking.Application.Abstractions.Auth;

namespace ClassBooking.Infrastructure.Auth;

internal sealed class BcryptPasswordHasher : IPasswordHasher
{
  public string Hash(string password) =>
      BCrypt.Net.BCrypt.EnhancedHashPassword(password, AuthConstants.BcryptWorkFactor);

  public bool Verify(string password, string hash) =>
      BCrypt.Net.BCrypt.EnhancedVerify(password, hash);
}
