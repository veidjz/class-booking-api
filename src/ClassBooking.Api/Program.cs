using ClassBooking.Api.Endpoints.Auth;
using ClassBooking.Api.Errors;
using ClassBooking.Api.Middleware;
using ClassBooking.Application;
using ClassBooking.Domain.Common;
using ClassBooking.Infrastructure;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables("CLASSBOOKING_");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

WebApplication app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
  app.MapOpenApi();
  app.MapScalarApiReference();
}

app.MapAuthEndpoints();

app.MapFallback((HttpContext httpContext) =>
    Result.Failure(TransportErrors.ResourceNotFound).ToProblem(httpContext));

app.Run();

public partial class Program;
