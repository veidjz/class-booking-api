namespace ClassBooking.Api.Errors;

/// <summary>
/// Shape of the "application/problem+json" body written by <see cref="ProblemDetailsMapper"/>,
/// declared so that the published contract carries the members a client keys on.
/// </summary>
internal record ErrorResponse(
    string Type,
    string Title,
    int Status,
    string Detail,
    string Instance,
    string ErrorCode,
    string TraceId);

/// <summary>
/// Shape of the body written when a request fails validation, which adds the failures by field.
/// The map is absent from the other errors answered with 400.
/// </summary>
internal sealed record ValidationErrorResponse(
    string Type,
    string Title,
    int Status,
    string Detail,
    string Instance,
    string ErrorCode,
    string TraceId,
    IReadOnlyDictionary<string, string[]>? Errors = null)
    : ErrorResponse(Type, Title, Status, Detail, Instance, ErrorCode, TraceId);
