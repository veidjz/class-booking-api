using System.Diagnostics;
using ClassBooking.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace ClassBooking.Api.Errors;

internal static class ResultExtensions
{
  internal static IResult ToProblem(this Result result, HttpContext httpContext)
  {
    if (result.IsSuccess)
    {
      throw new InvalidOperationException("A success result cannot be mapped to a problem response.");
    }

    ProblemDetails problemDetails = ProblemDetailsMapper.ToProblemDetails(
        result.Error,
        httpContext.Request.Path,
        Activity.Current?.Id ?? httpContext.TraceIdentifier);

    return Results.Problem(problemDetails);
  }
}
