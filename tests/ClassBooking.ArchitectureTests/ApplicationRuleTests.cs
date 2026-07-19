using System.Reflection;
using MediatR;
using NetArchTest.Rules;

namespace ClassBooking.ArchitectureTests;

public sealed class ApplicationRuleTests
{
  private static readonly Assembly Application = Assembly.Load("ClassBooking.Application");

  [Fact]
  public void Pipeline_behaviors_should_be_internal_and_sealed()
  {
    var result = Types.InAssembly(Application)
        .That()
        .ResideInNamespace("ClassBooking.Application.Behaviors")
        .And()
        .AreClasses()
        .Should()
        .BeSealed()
        .And()
        .NotBePublic()
        .GetResult();

    Assert.True(result.IsSuccessful, FailureMessage(result));
  }

  [Fact]
  public void Request_handlers_should_be_internal_and_sealed()
  {
    var offenders = Application.GetTypes()
        .Where(type => type.IsClass && ImplementsRequestHandler(type) && (type.IsPublic || !type.IsSealed))
        .Select(type => type.FullName)
        .ToArray();

    Assert.True(offenders.Length == 0, "Handlers must be internal sealed: " + string.Join(", ", offenders));
  }

  private static bool ImplementsRequestHandler(Type type) =>
      type.GetInterfaces().Any(candidate => candidate.IsGenericType
          && candidate.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));

  private static string FailureMessage(TestResult result) =>
      "Rule violated by: " + string.Join(", ", result.FailingTypes?.Select(type => type.FullName) ?? []);
}
