using ClassBooking.Api.Errors;
using ClassBooking.Api.Middleware;
using ClassBooking.Application;
using ClassBooking.Domain.Common;
using ClassBooking.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
  app.MapOpenApi();
  app.MapScalarApiReference();
}

app.MapFallback((HttpContext httpContext) =>
    Result.Failure(TransportErrors.ResourceNotFound).ToProblem(httpContext));

app.Run();

public partial class Program;
