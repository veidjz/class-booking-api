using System.Text.Json.Serialization;
using ClassBooking.Api.Endpoints.Auth;
using ClassBooking.Api.Errors;
using ClassBooking.Api.Middleware;
using ClassBooking.Api.OpenApi;
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
  options.SerializerOptions.Converters.Add(
      new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
  options.SerializerOptions.Converters.Add(new UtcInstantJsonConverter());
});

// A request the framework cannot bind reaches the exception handler in every environment,
// which is where it becomes a problem document and a log line.
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddRateLimiting(builder.Configuration);
builder.Services.AddOpenApi(options =>
{
  options.AddSchemaTransformer<StringSchemaTypeTransformer>();
  options.AddOperationTransformer<CreatedLocationHeaderTransformer>();
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

WebApplication app = builder.Build();

app.UseExceptionHandler();

app.UseStatusCodePages(async statusCodeContext =>
{
  HttpContext httpContext = statusCodeContext.HttpContext;
  if (UnmatchedRouteProblem.ShouldShape(httpContext))
  {
    await UnmatchedRouteProblem.WriteAsync(httpContext);
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
