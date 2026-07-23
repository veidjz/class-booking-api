using ClassBooking.Api.Errors;
using ClassBooking.Api.RateLimiting;
using ClassBooking.Application.Features.Accounts.Login;
using ClassBooking.Application.Features.Accounts.RegisterStudent;
using ClassBooking.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.RateLimiting;

namespace ClassBooking.Api.Endpoints.Auth;

internal static class AuthEndpoints
{
  private const string ProblemMediaType = "application/problem+json";

  internal static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder builder)
  {
    RouteGroupBuilder group = builder.MapGroup("/api/v1/auth");

    // Auth responses carry tokens; no cache anywhere between the API and the caller may keep them.
    group.AddEndpointFilter(async (invocationContext, next) =>
    {
      invocationContext.HttpContext.Response.Headers.CacheControl = "no-store";

      return await next(invocationContext);
    });

    group.MapPost("/register", RegisterStudentAsync)
        .WithName("RegisterStudent")
        .WithTags("Auth")
        .AllowAnonymous()
        .RequireRateLimiting(RateLimitPolicies.Auth)
        .Produces<RegisterStudentResponse>(StatusCodes.Status201Created)
        .Produces<ValidationErrorResponse>(StatusCodes.Status400BadRequest, ProblemMediaType)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict, ProblemMediaType)
        .Produces<ErrorResponse>(StatusCodes.Status429TooManyRequests, ProblemMediaType);

    group.MapPost("/login", LoginAsync)
        .WithName("Login")
        .WithTags("Auth")
        .AllowAnonymous()
        .RequireRateLimiting(RateLimitPolicies.Auth)
        .Produces<LoginResponse>(StatusCodes.Status200OK)
        .Produces<ValidationErrorResponse>(StatusCodes.Status400BadRequest, ProblemMediaType)
        .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized, ProblemMediaType)
        .Produces<ErrorResponse>(StatusCodes.Status429TooManyRequests, ProblemMediaType);

    return group;
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

  private static async Task<IResult> LoginAsync(
      LoginRequest request,
      ISender sender,
      HttpContext httpContext,
      CancellationToken cancellationToken)
  {
    LoginCommand command = new LoginCommand(request.Email, request.Password);

    Result<LoginResponse> result = await sender.Send(command, cancellationToken);
    if (result.IsFailure)
    {
      return result.ToProblem(httpContext);
    }

    return Results.Ok(result.Value);
  }
}
