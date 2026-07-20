using ClassBooking.Domain.Users;
using ClassBooking.IntegrationTests.Persistence.Fixtures;
using FluentAssertions;

namespace ClassBooking.IntegrationTests.Persistence;

[Collection(nameof(DatabaseCollection))]
public sealed class SchemaScopeTests(ContainersFixture fixture) : DatabaseTestBase(fixture)
{
  [Fact]
  public async Task should_create_only_the_account_tables()
  {
    IReadOnlyList<string> tables = await QueryAsync(
        "SELECT table_name FROM information_schema.tables WHERE table_schema = DATABASE()",
        reader => reader.GetString(0));

    tables.Should().BeEquivalentTo(["users", "students", "teachers", "__EFMigrationsHistory"]);
  }

  [Fact]
  public async Task should_record_the_add_accounts_migration()
  {
    IReadOnlyList<string> migrations = await QueryAsync(
        "SELECT migration_id FROM __EFMigrationsHistory",
        reader => reader.GetString(0));

    migrations.Should().ContainSingle().Which.Should().EndWith("AddAccounts");
  }

  [Fact]
  public async Task should_not_create_triggers_routines_or_views()
  {
    long triggers = await ScalarAsync<long>(
        "SELECT COUNT(*) FROM information_schema.triggers WHERE trigger_schema = DATABASE()");
    long routines = await ScalarAsync<long>(
        "SELECT COUNT(*) FROM information_schema.routines WHERE routine_schema = DATABASE()");
    long views = await ScalarAsync<long>(
        "SELECT COUNT(*) FROM information_schema.views WHERE table_schema = DATABASE()");

    triggers.Should().Be(0);
    routines.Should().Be(0);
    views.Should().Be(0);
  }

  [Fact]
  public async Task should_run_the_server_with_the_project_charset_and_engine()
  {
    string? charset = await ScalarAsync<string>("SELECT @@character_set_server");
    string? collation = await ScalarAsync<string>("SELECT @@collation_server");
    string? engine = await ScalarAsync<string>("SELECT @@default_storage_engine");

    charset.Should().Be("utf8mb4");
    collation.Should().Be("utf8mb4_0900_ai_ci");
    engine.Should().Be("InnoDB");
  }

  [Fact]
  public async Task should_clear_the_account_tables_and_keep_the_migration_history()
  {
    await AddAsync(User.CreateAdmin(
        "Root",
        "root@classbooking.dev",
        "hash",
        new DateTimeOffset(2026, 3, 2, 12, 0, 0, TimeSpan.Zero)));

    await Fixture.ResetAsync();

    long users = await ScalarAsync<long>("SELECT COUNT(*) FROM users");
    long students = await ScalarAsync<long>("SELECT COUNT(*) FROM students");
    long teachers = await ScalarAsync<long>("SELECT COUNT(*) FROM teachers");
    long migrations = await ScalarAsync<long>("SELECT COUNT(*) FROM __EFMigrationsHistory");

    users.Should().Be(0);
    students.Should().Be(0);
    teachers.Should().Be(0);
    migrations.Should().Be(1);
  }
}
