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

  /// <summary>Test-only signing key so hosts outside Development satisfy the startup validation.</summary>
  internal const string TestSigningKey =
      "CWAYL5NZgpzP7RBfvcpqb1rFP5oWgu5uxe4eL/CAtQgEbGFGKK42vJBWTMhCrOlSJkKX2qCTiypdDPXFz5vtuA==";

  internal static WebApplicationFactory<Program> Configure(
      this WebApplicationFactory<Program> root,
      string connectionString,
      string? environment = null,
      bool rateLimiting = false,
      Action<IServiceCollection>? configureServices = null) =>
      root.WithWebHostBuilder(builder =>
      {
        if (environment is not null)
        {
          builder.UseEnvironment(environment);
        }

        builder.UseSetting("ConnectionStrings:Database", connectionString);
        builder.UseSetting("RateLimiting:Enabled", rateLimiting ? "true" : "false");
        builder.UseSetting("Jwt:SigningKey", TestSigningKey);

        if (configureServices is not null)
        {
          builder.ConfigureTestServices(configureServices);
        }
      });
}
