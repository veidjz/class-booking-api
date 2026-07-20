using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace ClassBooking.IntegrationTests.Support;

internal static class ApiHost
{
  /// <summary>Database of tests that exercise the transport and never open a connection.</summary>
  internal const string UnusedConnectionString =
      "Server=localhost;Port=3306;Database=classbooking;User Id=classbooking;Password=classbooking";

  internal static WebApplicationFactory<Program> Configure(
      this WebApplicationFactory<Program> root,
      string connectionString,
      string? environment = null,
      Action<IServiceCollection>? configureServices = null) =>
      root.WithWebHostBuilder(builder =>
      {
        if (environment is not null)
        {
          builder.UseEnvironment(environment);
        }

        builder.UseSetting("ConnectionStrings:Database", connectionString);

        if (configureServices is not null)
        {
          builder.ConfigureTestServices(configureServices);
        }
      });
}
