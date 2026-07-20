using ClassBooking.Domain.Common;

namespace ClassBooking.Api.Errors;

internal static class TransportErrors
{
  internal static readonly Error ResourceNotFound =
      new("ResourceNotFound", "The requested resource was not found.");

  internal static readonly Error UnexpectedError =
      new("UnexpectedError", "An unexpected error occurred.");

  internal static readonly Error RateLimitExceeded =
      new("RateLimitExceeded", "Too many requests. Try again later.");

  internal static readonly Error MalformedRequest =
      new("ValidationFailed", "The request could not be read.");
}
