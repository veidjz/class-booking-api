using System.Diagnostics;
using ClassBooking.Api.Errors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ClassBooking.Api.Middleware;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
  public async ValueTask<bool> TryHandleAsync(
      HttpContext httpContext,
      Exception exception,
      CancellationToken cancellationToken)
  {
    logger.LogError(exception, "Unhandled exception for {RequestPath}", httpContext.Request.Path);

    ProblemDetails problemDetails = ProblemDetailsMapper.ToProblemDetails(
        TransportErrors.UnexpectedError,
        httpContext.Request.Path,
        Activity.Current?.Id ?? httpContext.TraceIdentifier);

    httpContext.Response.StatusCode = problemDetails.Status!.Value;
    await httpContext.Response.WriteAsJsonAsync(
        problemDetails,
        options: null,
        contentType: "application/problem+json",
        cancellationToken: cancellationToken);

    return true;
  }
}
