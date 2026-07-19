using ClassBooking.Domain.Users;
using ClassBooking.Infrastructure.Persistence;
using ClassBooking.IntegrationTests.Persistence.Fixtures;
using ClassBooking.IntegrationTests.Support;
using FluentAssertions;

namespace ClassBooking.IntegrationTests.Persistence;

[Collection(nameof(DatabaseCollection))]
public sealed class ApprovedDdlTests(ContainersFixture fixture) : DatabaseTestBase(fixture)
{
  private static readonly DateTimeOffset CreatedAt = new DateTimeOffset(2026, 3, 2, 12, 0, 0, TimeSpan.Zero);

  [Fact]
  public async Task should_match_the_approved_users_ddl()
  {
    IReadOnlyDictionary<string, (string ColumnType, string IsNullable, string? Default)> columns =
        await DescribeAsync("users");

    columns.Should().BeEquivalentTo(new Dictionary<string, (string, string, string?)>
    {
      ["id"] = ("binary(16)", "NO", null),
      ["name"] = ("varchar(120)", "NO", null),
      ["email"] = ("varchar(254)", "NO", null),
      ["password_hash"] = ("varchar(100)", "NO", null),
      ["role"] = ("varchar(30)", "NO", null),
      ["is_active"] = ("tinyint(1)", "NO", "1"),
      ["created_at"] = ("datetime(6)", "NO", null),
      ["version"] = ("int", "NO", "0"),
    });
  }

  [Fact]
  public async Task should_match_the_approved_students_ddl()
  {
    IReadOnlyDictionary<string, (string ColumnType, string IsNullable, string? Default)> columns =
        await DescribeAsync("students");

    columns.Should().BeEquivalentTo(new Dictionary<string, (string, string, string?)>
    {
      ["id"] = ("binary(16)", "NO", null),
    });
  }

  [Fact]
  public async Task should_match_the_approved_teachers_ddl()
  {
    IReadOnlyDictionary<string, (string ColumnType, string IsNullable, string? Default)> columns =
        await DescribeAsync("teachers");

    columns.Should().BeEquivalentTo(new Dictionary<string, (string, string, string?)>
    {
      ["id"] = ("binary(16)", "NO", null),
      ["cancellation_count"] = ("int", "NO", "0"),
      ["late_cancellation_count"] = ("int", "NO", "0"),
      ["no_show_count"] = ("int", "NO", "0"),
    });
  }

  [Fact]
  public async Task should_use_dynamic_row_format()
  {
    string? rowFormat = await ScalarAsync<string>(
        """
        SELECT row_format FROM information_schema.tables
        WHERE table_schema = DATABASE() AND table_name = 'users'
        """);

    rowFormat.Should().Be("Dynamic");
  }

  [Fact]
  public async Task should_write_the_version_column_in_the_insert_statement()
  {
    (AppDbContext context, CapturingCommandInterceptor interceptor) = CreateCapturingContext();
    await using (context)
    {
      context.Users.Add(Student.Register("Ana", "ana@classbooking.dev", "hash", CreatedAt));
      await context.SaveChangesAsync();
    }

    string insert = interceptor.Commands.Should().ContainSingle(command => command.Contains("INSERT INTO `users`")).Subject;

    insert.Should().Contain("version");
  }

  [Fact]
  public async Task should_write_the_is_active_column_when_the_account_is_created_inactive()
  {
    User admin = User.CreateAdmin("Root", "root@classbooking.dev", "hash", CreatedAt);
    admin.Deactivate(CreatedAt);

    (AppDbContext context, CapturingCommandInterceptor interceptor) = CreateCapturingContext();
    await using (context)
    {
      context.Users.Add(admin);
      await context.SaveChangesAsync();
    }

    string insert = interceptor.Commands.Should().ContainSingle(command => command.Contains("INSERT INTO `users`")).Subject;
    long isActive = await ScalarAsync<long>("SELECT is_active FROM users");

    insert.Should().Contain("is_active");
    isActive.Should().Be(0);
  }

  private async Task<IReadOnlyDictionary<string, (string ColumnType, string IsNullable, string? Default)>> DescribeAsync(
      string table)
  {
    IReadOnlyList<(string Column, string ColumnType, string IsNullable, string? Default)> rows = await QueryAsync(
        $"""
        SELECT column_name, column_type, is_nullable, column_default
        FROM information_schema.columns
        WHERE table_schema = DATABASE() AND table_name = '{table}'
        """,
        reader => (
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3)));

    return rows.ToDictionary(row => row.Column, row => (row.ColumnType, row.IsNullable, row.Default));
  }
}
