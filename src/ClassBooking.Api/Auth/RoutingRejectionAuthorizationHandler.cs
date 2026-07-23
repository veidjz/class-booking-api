using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace ClassBooking.Api.Auth;

internal sealed class RoutingRejectionAuthorizationHandler : IAuthorizationMiddlewareResultHandler
{
  private readonly AuthorizationMiddlewareResultHandler _inner = new AuthorizationMiddlewareResultHandler();

  public Task HandleAsync(
      RequestDelegate next,
      HttpContext context,
      AuthorizationPolicy policy,
      PolicyAuthorizationResult authorizeResult)
  {
    // Without this, the fallback policy turns routing rejections (unknown path, wrong verb,
    // unsupported content type) into 401 instead of their 404/405/415 transport status.
    if (!authorizeResult.Succeeded && IsRoutingRejection(context.GetEndpoint()))
    {
      return next(context);
    }

    return _inner.HandleAsync(next, context, policy, authorizeResult);
  }

  private static bool IsRoutingRejection(Endpoint? endpoint) =>
      endpoint is null || endpoint.Metadata.Count == 0;
}
