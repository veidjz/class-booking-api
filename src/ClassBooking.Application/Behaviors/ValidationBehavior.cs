using System.Text.Json;
using ClassBooking.Application.Common;
using ClassBooking.Domain.Common;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace ClassBooking.Application.Behaviors;

internal sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
  public async Task<TResponse> Handle(
      TRequest request,
      RequestHandlerDelegate<TResponse> next,
      CancellationToken cancellationToken)
  {
    if (!validators.Any())
    {
      return await next(cancellationToken);
    }

    ValidationContext<TRequest> context = new ValidationContext<TRequest>(request);
    ValidationResult[] validationResults = await Task.WhenAll(
        validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

    ValidationFailure[] failures = validationResults
        .SelectMany(validationResult => validationResult.Errors)
        .ToArray();

    if (failures.Length == 0)
    {
      return await next(cancellationToken);
    }

    Dictionary<string, string[]> errors = failures
        .GroupBy(failure => JsonNamingPolicy.CamelCase.ConvertName(failure.PropertyName))
        .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray());

    return ResultFactory.Failure<TResponse>(new ValidationError(errors));
  }
}
