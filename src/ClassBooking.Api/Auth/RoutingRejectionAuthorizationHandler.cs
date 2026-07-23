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
    if (!authorizeResult.Succeeded && IsRoutingRejection(context.GetEndpoint()))
    {
      // The fallback policy also reaches requests routing already rejected (unknown path, wrong
      // verb, unsupported content type); those keep their transport status instead of becoming 401.
      return next(context);
    }

    return _inner.HandleAsync(next, context, policy, authorizeResult);
  }

  // ponytail: rejection endpoints are the only ones the framework builds without metadata; match
  // the endpoints by type instead if a framework update ever changes that.
  private static bool IsRoutingRejection(Endpoint? endpoint) =>
      endpoint is null || endpoint.Metadata.Count == 0;
}
