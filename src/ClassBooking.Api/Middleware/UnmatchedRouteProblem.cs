using ClassBooking.Api.Errors;
using ClassBooking.Domain.Common;

namespace ClassBooking.Api.Middleware;

/// <summary>
/// Gives a problem document to the bodyless 404 that routing produces for a request that
/// matched no route. A status chosen by an endpoint belongs to that endpoint and is left alone.
/// </summary>
internal static class UnmatchedRouteProblem
{
  internal static bool ShouldShape(HttpContext httpContext) =>
      httpContext.Response.StatusCode == StatusCodes.Status404NotFound
      && httpContext.GetEndpoint() is null;

  internal static Task WriteAsync(HttpContext httpContext) =>
      Result.Failure(TransportErrors.ResourceNotFound).ToProblem(httpContext).ExecuteAsync(httpContext);
}
