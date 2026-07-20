using ClassBooking.IntegrationTests.Persistence.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClassBooking.IntegrationTests.Api;

[Collection(nameof(DatabaseCollection))]
public sealed class ConfigurationSourceTests
{
  private const string PrefixedVariable = "CLASSBOOKING_ConnectionStrings__Database";
  private const string PrefixedValue = "Server=prefixed;Database=classbooking;User Id=classbooking;Password=classbooking";
  private const string LocalValue = "Server=localhost;Port=3306;Database=classbooking;User Id=classbooking;Password=classbooking";

  [Fact]
  public void should_read_the_connection_string_from_the_prefixed_environment_variable()
  {
    Environment.SetEnvironmentVariable(PrefixedVariable, PrefixedValue);
    try
    {
      using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>();

      IConfiguration configuration = factory.Services.GetRequiredService<IConfiguration>();

      configuration.GetConnectionString("Database").Should().Be(PrefixedValue);
    }
    finally
    {
      Environment.SetEnvironmentVariable(PrefixedVariable, null);
    }
  }

  [Fact]
  public void should_let_the_prefixed_environment_variable_win_over_the_settings_file()
  {
    Environment.SetEnvironmentVariable(PrefixedVariable, PrefixedValue);
    try
    {
      using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
          .WithWebHostBuilder(builder => builder.UseEnvironment("Development"));

      IConfiguration configuration = factory.Services.GetRequiredService<IConfiguration>();

      configuration.GetConnectionString("Database").Should().Be(PrefixedValue);
    }
    finally
    {
      Environment.SetEnvironmentVariable(PrefixedVariable, null);
    }
  }

  [Fact]
  public void should_keep_the_local_connection_string_out_of_the_base_settings_file()
  {
    using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));

    Func<IServiceProvider> build = () => factory.Services;

    build.Should().Throw<InvalidOperationException>()
        .WithMessage("*connection string 'Database' is not configured*");
  }

  [Fact]
  public void should_read_the_local_connection_string_from_the_development_settings_file()
  {
    using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder => builder.UseEnvironment("Development"));

    IConfiguration configuration = factory.Services.GetRequiredService<IConfiguration>();

    configuration.GetConnectionString("Database").Should().Be(LocalValue);
  }
}
