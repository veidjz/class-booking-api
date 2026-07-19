using ClassBooking.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClassBooking.Application.Behaviors;

internal sealed class RequestLoggingBehavior<TRequest, TResponse>(
    ILogger<RequestLoggingBehavior<TRequest, TResponse>> logger,
    TimeProvider clock) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
  public async Task<TResponse> Handle(
      TRequest request,
      RequestHandlerDelegate<TResponse> next,
      CancellationToken cancellationToken)
  {
    var requestName = typeof(TRequest).Name;
    using var scope = logger.BeginScope(new Dictionary<string, object> { ["RequestName"] = requestName });

    logger.LogInformation("Handling {RequestName}", requestName);
    var startTimestamp = clock.GetTimestamp();

    var response = await next(cancellationToken);

    var elapsedMilliseconds = clock.GetElapsedTime(startTimestamp).TotalMilliseconds;
    if (response.IsSuccess)
    {
      logger.LogInformation(
          "Handled {RequestName} in {ElapsedMilliseconds}ms", requestName, elapsedMilliseconds);
    }
    else
    {
      logger.LogInformation(
          "Business rule failed with {ErrorCode} for {RequestName} in {ElapsedMilliseconds}ms",
          response.Error.Code, requestName, elapsedMilliseconds);
    }

    return response;
  }
}
