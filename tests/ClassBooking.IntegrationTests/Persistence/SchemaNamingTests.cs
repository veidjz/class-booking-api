using ClassBooking.Domain.Users;
using ClassBooking.IntegrationTests.Persistence.Fixtures;
using FluentAssertions;
using MySqlConnector;

namespace ClassBooking.IntegrationTests.Persistence;

[Collection(nameof(DatabaseCollection))]
public sealed class SchemaNamingTests(ContainersFixture fixture) : DatabaseTestBase(fixture)
{
  private static readonly DateTimeOffset CreatedAt = new DateTimeOffset(2026, 3, 2, 12, 0, 0, TimeSpan.Zero);

  [Fact]
  public async Task should_name_the_email_unique_index_ux_users_email()
  {
    IReadOnlyList<(string Index, string Column, long NonUnique)> indexes = await QueryAsync(
        """
        SELECT index_name, column_name, non_unique FROM information_schema.statistics
        WHERE table_schema = DATABASE() AND table_name = 'users' AND index_name = 'ux_users_email'
        """,
        reader => (reader.GetString(0), reader.GetString(1), reader.GetInt64(2)));

    indexes.Should().ContainSingle();
    indexes[0].Column.Should().Be("email");
    indexes[0].NonUnique.Should().Be(0);
  }

  [Fact]
  public async Task should_name_the_subtype_foreign_keys()
  {
    IReadOnlyList<string> constraints = await QueryAsync(
        """
        SELECT constraint_name FROM information_schema.table_constraints
        WHERE table_schema = DATABASE() AND constraint_type = 'FOREIGN KEY'
        """,
        reader => reader.GetString(0));

    constraints.Should().BeEquivalentTo(["fk_students_users", "fk_teachers_users"]);
  }

  [Fact]
  public async Task should_declare_the_teacher_counters_check()
  {
    IReadOnlyList<string> checks = await QueryAsync(
        """
        SELECT constraint_name FROM information_schema.table_constraints
        WHERE table_schema = DATABASE() AND constraint_type = 'CHECK'
        """,
        reader => reader.GetString(0));

    checks.Should().Contain("chk_teachers_counters");
  }

  [Fact]
  public async Task should_reject_negative_teacher_counters()
  {
    await AddAsync(Teacher.Create("Paulo", "paulo@classbooking.dev", "hash", CreatedAt));

    Func<Task> update = () => ExecuteAsync("UPDATE teachers SET no_show_count = -1");

    await update.Should().ThrowAsync<MySqlException>();
  }

  [Fact]
  public async Task should_not_use_ef_default_constraint_prefixes()
  {
    IReadOnlyList<string> names = await QueryAsync(
        """
        SELECT constraint_name FROM information_schema.table_constraints
        WHERE table_schema = DATABASE()
        UNION
        SELECT index_name FROM information_schema.statistics WHERE table_schema = DATABASE()
        """,
        reader => reader.GetString(0));

    names.Should().NotContain(name =>
        name.StartsWith("IX_", StringComparison.Ordinal)
        || name.StartsWith("FK_", StringComparison.Ordinal)
        || name.StartsWith("AK_", StringComparison.Ordinal)
        || name.StartsWith("CK_", StringComparison.Ordinal));
  }

  [Fact]
  public async Task should_not_create_indexes_beyond_the_catalog()
  {
    IReadOnlyList<string> indexes = await QueryAsync(
        """
        SELECT DISTINCT index_name FROM information_schema.statistics
        WHERE table_schema = DATABASE()
        """,
        reader => reader.GetString(0));

    indexes.Should().BeEquivalentTo(["PRIMARY", "ux_users_email"]);
  }

  [Fact]
  public async Task should_restrict_deletes_on_the_subtype_foreign_keys()
  {
    IReadOnlyList<(string Name, string DeleteRule, string UpdateRule)> rules = await QueryAsync(
        """
        SELECT constraint_name, delete_rule, update_rule FROM information_schema.referential_constraints
        WHERE constraint_schema = DATABASE()
        """,
        reader => (reader.GetString(0), reader.GetString(1), reader.GetString(2)));

    rules.Should().HaveCount(2);
    rules.Should().OnlyContain(rule =>
        (rule.DeleteRule == "RESTRICT" || rule.DeleteRule == "NO ACTION")
        && (rule.UpdateRule == "RESTRICT" || rule.UpdateRule == "NO ACTION"));
  }

  private async Task ExecuteAsync(string sql)
  {
    await using MySqlConnection connection = new MySqlConnection(Fixture.ConnectionString);
    await connection.OpenAsync();
    await using MySqlCommand command = new MySqlCommand(sql, connection);
    await command.ExecuteNonQueryAsync();
  }
}
