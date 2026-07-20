using System.Text.Json.Serialization;
using ClassBooking.Api.Endpoints.Auth;
using ClassBooking.Api.Errors;
using ClassBooking.Api.Middleware;
using ClassBooking.Api.RateLimiting;
using ClassBooking.Api.Serialization;
using ClassBooking.Application;
using ClassBooking.Domain.Common;
using ClassBooking.Infrastructure;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables("CLASSBOOKING_");

builder.Services.ConfigureHttpJsonOptions(options =>
{
  options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
  options.SerializerOptions.Converters.Add(new UtcInstantJsonConverter());
});

builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddRateLimiting(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

WebApplication app = builder.Build();

app.UseExceptionHandler();

app.UseStatusCodePages(async statusCodeContext =>
{
  HttpContext httpContext = statusCodeContext.HttpContext;
  Error? error = TransportErrors.ForStatusCode(httpContext.Response.StatusCode);
  if (error is not null)
  {
    await Result.Failure(error).ToProblem(httpContext).ExecuteAsync(httpContext);
  }
});

app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
  app.MapOpenApi();
  app.MapScalarApiReference();
}

app.MapAuthEndpoints();

app.Run();

public partial class Program;
