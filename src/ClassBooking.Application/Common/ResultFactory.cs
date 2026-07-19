using System.Reflection;
using ClassBooking.Domain.Common;

namespace ClassBooking.Application.Common;

internal static class ResultFactory
{
  internal static TResult Failure<TResult>(Error error)
      where TResult : Result =>
      FailureFactory<TResult>.Create(error);

  private static class FailureFactory<TResult>
      where TResult : Result
  {
    internal static readonly Func<Error, TResult> Create = Build();

    private static Func<Error, TResult> Build()
    {
      if (typeof(TResult) == typeof(Result))
      {
        return error => (TResult)Result.Failure(error);
      }

      var valueType = typeof(TResult).GetGenericArguments().Single();
      var failure = typeof(Result)
          .GetMethods(BindingFlags.Public | BindingFlags.Static)
          .Single(method => method.Name == nameof(Result.Failure) && method.IsGenericMethodDefinition)
          .MakeGenericMethod(valueType);

      return error => (TResult)failure.Invoke(null, [error])!;
    }
  }
}
