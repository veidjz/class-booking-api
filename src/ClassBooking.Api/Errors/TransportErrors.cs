using ClassBooking.Domain.Common;

namespace ClassBooking.Api.Errors;

internal static class TransportErrors
{
  internal static readonly Error ResourceNotFound =
      new("ResourceNotFound", "The requested resource was not found.");

  internal static readonly Error UnexpectedError =
      new("UnexpectedError", "An unexpected error occurred.");
}
