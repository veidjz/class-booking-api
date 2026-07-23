using System.Text.Json.Serialization;
using ClassBooking.Api.Auth;
using ClassBooking.Api.Endpoints.Auth;
using ClassBooking.Api.Errors;
using ClassBooking.Api.Middleware;
using ClassBooking.Api.OpenApi;
using ClassBooking.Api.RateLimiting;
using ClassBooking.Api.Serialization;
using ClassBooking.Application;
using ClassBooking.Domain.Common;
using ClassBooking.Infrastructure;
using ClassBooking.Infrastructure.Auth;
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

builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(
        options => options.HasValidSigningKey(),
        "The 'Jwt:SigningKey' must be Base64 content decoding to at least 32 bytes.")
    .ValidateOnStart();
builder.Services.AddApiAuthentication();
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

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
  app.MapOpenApi().AllowAnonymous();
  app.MapScalarApiReference().AllowAnonymous();
}

app.MapAuthEndpoints();

app.Run();

public partial class Program;
