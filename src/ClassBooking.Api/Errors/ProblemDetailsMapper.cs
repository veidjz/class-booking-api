using System.Collections.Frozen;
using System.Text;
using ClassBooking.Application.Common;
using ClassBooking.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace ClassBooking.Api.Errors;

internal static class ProblemDetailsMapper
{
  private const string TypeUriPrefix = "https://veidjz.github.io/class-booking-api/errors/";

  private static readonly FrozenDictionary<string, int> StatusByCode = new Dictionary<string, int>
  {
    ["ValidationFailed"] = StatusCodes.Status400BadRequest,
    ["ReasonRequired"] = StatusCodes.Status400BadRequest,
    ["InvalidAvailabilityRule"] = StatusCodes.Status400BadRequest,
    ["InvalidAvailabilityBlock"] = StatusCodes.Status400BadRequest,
    ["InvalidCredentials"] = StatusCodes.Status401Unauthorized,
    ["PaymentDeclined"] = StatusCodes.Status402PaymentRequired,
    ["PermissionDenied"] = StatusCodes.Status403Forbidden,
    ["BookingNotFound"] = StatusCodes.Status404NotFound,
    ["TeacherNotFound"] = StatusCodes.Status404NotFound,
    ["UserNotFound"] = StatusCodes.Status404NotFound,
    ["AvailabilityRuleNotFound"] = StatusCodes.Status404NotFound,
    ["AvailabilityBlockNotFound"] = StatusCodes.Status404NotFound,
    ["ResourceNotFound"] = StatusCodes.Status404NotFound,
    ["SlotAlreadyBooked"] = StatusCodes.Status409Conflict,
    ["StudentScheduleConflict"] = StatusCodes.Status409Conflict,
    ["ActiveBookingLimitExceeded"] = StatusCodes.Status409Conflict,
    ["InvalidStateTransition"] = StatusCodes.Status409Conflict,
    ["EmailAlreadyInUse"] = StatusCodes.Status409Conflict,
    ["BookingWindowViolation"] = StatusCodes.Status422UnprocessableEntity,
    ["CancellationNotAllowed"] = StatusCodes.Status422UnprocessableEntity,
    ["ConfirmationWindowExpired"] = StatusCodes.Status422UnprocessableEntity,
    ["NoShowWindowClosed"] = StatusCodes.Status422UnprocessableEntity,
    ["ReclassificationWindowClosed"] = StatusCodes.Status422UnprocessableEntity,
    ["RescheduleLimitReached"] = StatusCodes.Status422UnprocessableEntity,
    ["RescheduleNotAllowed"] = StatusCodes.Status422UnprocessableEntity,
    ["SlotNotAvailable"] = StatusCodes.Status422UnprocessableEntity,
    ["SelfDeactivationNotAllowed"] = StatusCodes.Status422UnprocessableEntity,
    ["RateLimitExceeded"] = StatusCodes.Status429TooManyRequests,
    ["UnexpectedError"] = StatusCodes.Status500InternalServerError,
  }.ToFrozenDictionary();

  internal static ProblemDetails ToProblemDetails(Error error, string? instance, string? traceId)
  {
    int status = StatusByCode.GetValueOrDefault(error.Code, StatusCodes.Status500InternalServerError);

    ProblemDetails problemDetails = new ProblemDetails
    {
      Type = TypeUriPrefix + ToKebabCase(error.Code),
      Title = ToTitle(error.Code),
      Status = status,
      Detail = error.Message,
      Instance = instance,
    };

    problemDetails.Extensions["errorCode"] = error.Code;

    if (traceId is not null)
    {
      problemDetails.Extensions["traceId"] = traceId;
    }

    if (error is ValidationError validationError)
    {
      problemDetails.Extensions["errors"] = validationError.Errors;
    }

    return problemDetails;
  }

  private static string ToKebabCase(string code) => ConvertCode(code, '-', lowerFirstWordOnly: false);

  private static string ToTitle(string code) => ConvertCode(code, ' ', lowerFirstWordOnly: true);

  private static string ConvertCode(string code, char separator, bool lowerFirstWordOnly)
  {
    StringBuilder builder = new StringBuilder(code.Length + 8);

    for (int index = 0; index < code.Length; index++)
    {
      char character = code[index];
      if (index > 0 && char.IsUpper(character) && StartsNewWord(code, index))
      {
        builder.Append(separator);
      }

      bool keepUpper = lowerFirstWordOnly && index == 0;
      builder.Append(keepUpper ? character : char.ToLowerInvariant(character));
    }

    return builder.ToString();
  }

  private static bool StartsNewWord(string code, int index) =>
      char.IsLower(code[index - 1])
      || (index + 1 < code.Length && char.IsLower(code[index + 1]));
}
