using System.Diagnostics;
using ClassBooking.Api.Errors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ClassBooking.Api.Middleware;

internal sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
  public async ValueTask<bool> TryHandleAsync(
      HttpContext httpContext,
      Exception exception,
      CancellationToken cancellationToken)
  {
    bool malformed = exception is BadHttpRequestException { StatusCode: StatusCodes.Status400BadRequest };
    if (malformed)
    {
      logger.LogWarning(exception, "Request could not be read for {RequestPath}", httpContext.Request.Path);
    }
    else
    {
      logger.LogError(exception, "Unhandled exception for {RequestPath}", httpContext.Request.Path);
    }

    ProblemDetails problemDetails = ProblemDetailsMapper.ToProblemDetails(
        malformed ? TransportErrors.MalformedRequest : TransportErrors.UnexpectedError,
        httpContext.Request.Path,
        Activity.Current?.Id ?? httpContext.TraceIdentifier);

    httpContext.Response.StatusCode = problemDetails.Status!.Value;

    return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
    {
      HttpContext = httpContext,
      ProblemDetails = problemDetails,
    });
  }
}
