using ClassBooking.Application.Abstractions.Data;
using ClassBooking.Infrastructure.Persistence;
using ClassBooking.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClassBooking.Infrastructure;

public static class DependencyInjection
{
  public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
  {
    string connectionString = configuration.GetConnectionString("Database")
        ?? throw new InvalidOperationException("The connection string 'Database' is not configured.");

    services.AddSingleton<VersionTokenInterceptor>();

    services.AddDbContextPool<AppDbContext>((serviceProvider, options) =>
        options
            .UseMySql(
                connectionString,
                new MySqlServerVersion(new Version(8, 4, 0)),
                mySql => mySql.MigrationsAssembly("ClassBooking.Infrastructure"))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(serviceProvider.GetRequiredService<VersionTokenInterceptor>()));

    services.AddScoped<IAppDbContext>(serviceProvider => serviceProvider.GetRequiredService<AppDbContext>());

    return services;
  }
}
