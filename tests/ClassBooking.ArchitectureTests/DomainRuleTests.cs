using System.Reflection;

namespace ClassBooking.ArchitectureTests;

public sealed class DomainRuleTests
{
  private static readonly Assembly Domain = Assembly.Load("ClassBooking.Domain");

  [Fact]
  public void Domain_should_reference_only_bcl_and_mediatr_contracts()
  {
    var forbidden = Domain.GetReferencedAssemblies()
        .Select(reference => reference.Name!)
        .Where(name => name != "MediatR.Contracts"
            && name != "netstandard"
            && name != "mscorlib"
            && !name.StartsWith("System", StringComparison.Ordinal))
        .ToArray();

    Assert.True(forbidden.Length == 0, "Forbidden Domain references: " + string.Join(", ", forbidden));
  }

  [Fact]
  public void Domain_should_not_define_exception_types()
  {
    var exceptionTypes = Domain.GetTypes()
        .Where(type => typeof(Exception).IsAssignableFrom(type))
        .Select(type => type.FullName)
        .ToArray();

    Assert.True(exceptionTypes.Length == 0, "Exception types found in Domain: " + string.Join(", ", exceptionTypes));
  }
}
