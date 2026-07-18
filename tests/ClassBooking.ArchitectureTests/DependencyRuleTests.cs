using System.Reflection;
using NetArchTest.Rules;

namespace ClassBooking.ArchitectureTests;

public sealed class DependencyRuleTests
{
  private static readonly Assembly Domain = Assembly.Load("ClassBooking.Domain");
  private static readonly Assembly Application = Assembly.Load("ClassBooking.Application");

  private const string ApplicationNamespace = "ClassBooking.Application";
  private const string InfrastructureNamespace = "ClassBooking.Infrastructure";
  private const string ApiNamespace = "ClassBooking.Api";
  private const string WorkerNamespace = "ClassBooking.Worker";

  [Fact]
  public void Domain_should_not_depend_on_other_layers()
  {
    var result = Types.InAssembly(Domain)
        .Should()
        .NotHaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, ApiNamespace, WorkerNamespace)
        .GetResult();

    Assert.True(result.IsSuccessful, FailureMessage(result));
  }

  [Fact]
  public void Application_should_not_depend_on_infrastructure_or_hosts()
  {
    var result = Types.InAssembly(Application)
        .Should()
        .NotHaveDependencyOnAny(InfrastructureNamespace, ApiNamespace, WorkerNamespace)
        .GetResult();

    Assert.True(result.IsSuccessful, FailureMessage(result));
  }

  [Fact]
  public void Application_should_not_depend_on_infrastructure_drivers()
  {
    var result = Types.InAssembly(Application)
        .Should()
        .NotHaveDependencyOnAny("Pomelo", "MySqlConnector", "StackExchange.Redis", "RabbitMQ", "Hangfire")
        .GetResult();

    Assert.True(result.IsSuccessful, FailureMessage(result));
  }

  private static string FailureMessage(TestResult result) =>
      "Dependency rule violated by: " + string.Join(", ", result.FailingTypes?.Select(t => t.FullName) ?? []);
}
