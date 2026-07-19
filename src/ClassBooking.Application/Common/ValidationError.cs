using ClassBooking.Domain.Common;

namespace ClassBooking.Application.Common;

public sealed record ValidationError(IReadOnlyDictionary<string, string[]> Errors)
    : Error("ValidationFailed", "One or more validation errors occurred.");
