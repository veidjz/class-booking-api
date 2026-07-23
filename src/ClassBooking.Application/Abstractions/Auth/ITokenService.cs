using ClassBooking.Domain.Users;

namespace ClassBooking.Application.Abstractions.Auth;

public interface ITokenService
{
  AccessToken Issue(User user);
}
