using ClassBooking.Application;
using ClassBooking.Infrastructure;
using ClassBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using Respawn;
using Respawn.Graph;
using Testcontainers.MySql;

namespace ClassBooking.IntegrationTests.Persistence.Fixtures;

public sealed class ContainersFixture : IAsyncLifetime
{
  private readonly MySqlContainer _container = new MySqlBuilder()
      .WithImage("mysql:8.4")
      .WithDatabase("classbooking")
      .WithUsername("classbooking")
      .WithPassword("classbooking")
      .WithCommand("--character-set-server=utf8mb4", "--collation-server=utf8mb4_0900_ai_ci")
      .Build();

  private ServiceProvider? _provider;
  private Respawner? _respawner;

  public string ConnectionString => _container.GetConnectionString();

  public async Task InitializeAsync()
  {
    try
    {
      await _container.StartAsync();
    }
    catch (Exception exception)
    {
      throw new InvalidOperationException("Docker is required to run the integration tests.", exception);
    }

    IConfiguration configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Database"] = ConnectionString })
        .Build();

    ServiceCollection services = new ServiceCollection();
    services.AddSingleton(configuration);
    services.AddLogging();
    services.AddApplication();
    services.AddInfrastructure(configuration);
    _provider = services.BuildServiceProvider();

    using (IServiceScope scope = _provider.CreateScope())
    {
      AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
      await context.Database.MigrateAsync();
    }

    await using MySqlConnection connection = new MySqlConnection(ConnectionString);
    await connection.OpenAsync();
    _respawner = await Respawner.CreateAsync(
        connection,
        new RespawnerOptions
        {
          DbAdapter = DbAdapter.MySql,
          SchemasToInclude = ["classbooking"],
          TablesToIgnore = [new Table("__EFMigrationsHistory")],
        });
  }

  public IServiceScope CreateScope() => _provider!.CreateScope();

  public async Task ResetAsync()
  {
    await using MySqlConnection connection = new MySqlConnection(ConnectionString);
    await connection.OpenAsync();
    await _respawner!.ResetAsync(connection);
  }

  public async Task DisposeAsync()
  {
    if (_provider is not null)
    {
      await _provider.DisposeAsync();
    }

    await _container.DisposeAsync();
  }
}
