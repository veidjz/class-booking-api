namespace ClassBooking.Infrastructure.Auth;

internal static class AuthConstants
{
  internal const int BcryptWorkFactor = 12;

  internal static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(60);
}
