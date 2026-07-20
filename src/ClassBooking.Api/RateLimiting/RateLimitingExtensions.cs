using System.Globalization;
using System.Threading.RateLimiting;
using ClassBooking.Api.Errors;
using ClassBooking.Domain.Common;
using Microsoft.AspNetCore.RateLimiting;

namespace ClassBooking.Api.RateLimiting;

internal static class RateLimitingExtensions
{
  private const int GlobalPermitLimit = 100;
  private const int AuthPermitLimit = 5;
  private const string LoggerCategory = "ClassBooking.Api.RateLimiting";
  private const string UnknownClient = "unknown";

  private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

  internal static IServiceCollection AddRateLimiting(
      this IServiceCollection services,
      IConfiguration configuration)
  {
    bool enabled = configuration.GetValue("RateLimiting:Enabled", defaultValue: true);

    return services.AddRateLimiter(options =>
    {
      options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

      options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
          httpContext => Partition(httpContext, enabled, GlobalPermitLimit));

      options.AddPolicy(
          RateLimitPolicies.Auth,
          httpContext => Partition(httpContext, enabled, AuthPermitLimit));

      options.OnRejected = OnRejectedAsync;
    });
  }

  /// <summary>
  /// Partitions by client address, which behind the proxy comes from the forwarded headers.
  /// A disabled limiter still resolves every policy, so no route loses its metadata in tests.
  /// </summary>
  private static RateLimitPartition<string> Partition(HttpContext httpContext, bool enabled, int permitLimit)
  {
    if (!enabled)
    {
      return RateLimitPartition.GetNoLimiter(UnknownClient);
    }

    string client = httpContext.Connection.RemoteIpAddress?.ToString() ?? UnknownClient;

    return RateLimitPartition.GetFixedWindowLimiter(client, _ => new FixedWindowRateLimiterOptions
    {
      PermitLimit = permitLimit,
      Window = Window,
      QueueLimit = 0,
    });
  }

  private static ValueTask OnRejectedAsync(OnRejectedContext context, CancellationToken cancellationToken)
  {
    HttpContext httpContext = context.HttpContext;

    TimeSpan retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan leased)
        ? leased
        : Window;
    httpContext.Response.Headers.RetryAfter =
        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);

    ILogger logger = httpContext.RequestServices
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger(LoggerCategory);
    logger.LogWarning(
        "Rate limit rejected {Method} {Path}",
        httpContext.Request.Method,
        httpContext.Request.Path.Value);

    return new ValueTask(
        Result.Failure(TransportErrors.RateLimitExceeded).ToProblem(httpContext).ExecuteAsync(httpContext));
  }
}
