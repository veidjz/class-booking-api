using ClassBooking.Application.Abstractions.Auth;
using ClassBooking.Application.Abstractions.Messaging;
using ClassBooking.Application.Common;
using ClassBooking.Domain.Common;
using ClassBooking.Domain.Users;
using Microsoft.Extensions.Logging;

namespace ClassBooking.Application.Features.Accounts.Login;

internal sealed class LoginCommandHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    ILogger<LoginCommandHandler> logger)
    : ICommandHandler<LoginCommand, LoginResponse>
{
  internal const string GhostPasswordHash = "$2a$12$x23wNuhIQznIfjLxCKsIYeua9IYT8aas2wDHeD6uA.haVv6zCumtG";

  public async Task<Result<LoginResponse>> Handle(LoginCommand command, CancellationToken cancellationToken)
  {
    string email = command.Email.Trim().ToLowerInvariant();
    User? user = await users.GetByEmailAsync(email, cancellationToken);

    if (user is null)
    {
      passwordHasher.Verify(command.Password, GhostPasswordHash);

      return Result.Failure<LoginResponse>(AuthenticationErrors.InvalidCredentials);
    }

    // Verify before the activation check, so a deactivated account cannot answer faster by timing.
    if (!passwordHasher.Verify(command.Password, user.PasswordHash) || !user.IsActive)
    {
      return Result.Failure<LoginResponse>(AuthenticationErrors.InvalidCredentials);
    }

    if (passwordHasher.NeedsRehash(user.PasswordHash))
    {
      user.RehashPassword(passwordHasher.Hash(command.Password));
    }

    AccessToken accessToken = tokenService.Issue(user);
    logger.LogInformation("Login succeeded for {UserId}", user.Id);

    return Result.Success(new LoginResponse("Bearer", accessToken.Token, accessToken.ExpiresInSeconds));
  }
}
