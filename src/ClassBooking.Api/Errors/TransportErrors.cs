using ClassBooking.Domain.Common;

namespace ClassBooking.Api.Errors;

internal static class TransportErrors
{
  internal static readonly Error ResourceNotFound =
      new("ResourceNotFound", "The requested resource was not found.");

  internal static readonly Error UnexpectedError =
      new("UnexpectedError", "An unexpected error occurred.");

  internal static readonly Error MalformedRequest =
      new("ValidationFailed", "The request body could not be read.");

  internal static Error? ForStatusCode(int statusCode) => statusCode switch
  {
    StatusCodes.Status400BadRequest => MalformedRequest,
    StatusCodes.Status404NotFound => ResourceNotFound,
    _ => null,
  };
}
