using ClassBooking.Api.Errors;
using ClassBooking.Api.RateLimiting;
using ClassBooking.Application.Features.Accounts.RegisterStudent;
using ClassBooking.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.RateLimiting;

namespace ClassBooking.Api.Endpoints.Auth;

internal static class AuthEndpoints
{
  internal static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder builder)
  {
    RouteGroupBuilder group = builder.MapGroup("/api/v1/auth");

    group.MapPost("/register", RegisterStudentAsync)
        .AllowAnonymous()
        .RequireRateLimiting(RateLimitPolicies.Auth)
        .Produces<RegisterStudentResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict);

    return builder;
  }

  private static async Task<IResult> RegisterStudentAsync(
      RegisterStudentRequest request,
      ISender sender,
      HttpContext httpContext,
      CancellationToken cancellationToken)
  {
    RegisterStudentCommand command = new RegisterStudentCommand(request.Name, request.Email, request.Password);

    Result<RegisterStudentResponse> result = await sender.Send(command, cancellationToken);
    if (result.IsFailure)
    {
      return result.ToProblem(httpContext);
    }

    return Results.Created($"/api/v1/students/{result.Value.Id}", result.Value);
  }
}
