using System.Diagnostics;
using ClassBooking.Domain.Common;

namespace ClassBooking.Api.Errors;

internal static class ResultExtensions
{
  internal static IResult ToProblem(this Result result, HttpContext httpContext)
  {
    if (result.IsSuccess)
    {
      throw new InvalidOperationException("A success result cannot be mapped to a problem response.");
    }

    var problemDetails = ProblemDetailsMapper.ToProblemDetails(
        result.Error,
        httpContext.Request.Path,
        Activity.Current?.Id ?? httpContext.TraceIdentifier);

    return Results.Problem(problemDetails);
  }
}
