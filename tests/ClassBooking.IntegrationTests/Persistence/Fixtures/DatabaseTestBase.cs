using ClassBooking.Domain.Users;
using ClassBooking.Infrastructure.Persistence;
using ClassBooking.IntegrationTests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;

namespace ClassBooking.IntegrationTests.Persistence.Fixtures;

public abstract class DatabaseTestBase(ContainersFixture fixture) : IAsyncLifetime
{
  protected ContainersFixture Fixture { get; } = fixture;

  public Task InitializeAsync() => Fixture.ResetAsync();

  public Task DisposeAsync() => Task.CompletedTask;

  protected IServiceScope CreateScope() => Fixture.CreateScope();

  protected async Task AddAsync(params User[] users)
  {
    using IServiceScope scope = CreateScope();
    AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Users.AddRange(users);
    await context.SaveChangesAsync();
  }

  protected (AppDbContext Context, CapturingCommandInterceptor Interceptor) CreateCapturingContext()
  {
    CapturingCommandInterceptor interceptor = new CapturingCommandInterceptor();
    using IServiceScope scope = CreateScope();
    DbContextOptions<AppDbContext> composed =
        scope.ServiceProvider.GetRequiredService<DbContextOptions<AppDbContext>>();
    DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>(composed)
        .AddInterceptors(interceptor)
        .Options;

    return (new AppDbContext(options), interceptor);
  }

  protected async Task<TValue?> ScalarAsync<TValue>(string sql)
  {
    await using MySqlConnection connection = new MySqlConnection(Fixture.ConnectionString);
    await connection.OpenAsync();
    await using MySqlCommand command = new MySqlCommand(sql, connection);
    object? value = await command.ExecuteScalarAsync();

    return value is null or DBNull ? default : (TValue)Convert.ChangeType(value, typeof(TValue));
  }

  protected async Task<IReadOnlyList<TRow>> QueryAsync<TRow>(string sql, Func<MySqlDataReader, TRow> map)
  {
    await using MySqlConnection connection = new MySqlConnection(Fixture.ConnectionString);
    await connection.OpenAsync();
    await using MySqlCommand command = new MySqlCommand(sql, connection);
    await using MySqlDataReader reader = await command.ExecuteReaderAsync();

    List<TRow> rows = [];
    while (await reader.ReadAsync())
    {
      rows.Add(map(reader));
    }

    return rows;
  }
}
