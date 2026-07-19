using ClassBooking.Domain.Users;
using ClassBooking.Infrastructure.Persistence;
using ClassBooking.IntegrationTests.Persistence.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClassBooking.IntegrationTests.Persistence;

[Collection(nameof(DatabaseCollection))]
public sealed class ValueConversionTests(ContainersFixture fixture) : DatabaseTestBase(fixture)
{
  private static readonly DateTimeOffset CreatedAt = new DateTimeOffset(2026, 3, 2, 12, 0, 0, TimeSpan.Zero);

  [Fact]
  public async Task should_store_the_identity_as_canonical_big_endian_bytes()
  {
    Student student = Student.Register("Ana", "ana@classbooking.dev", "hash", CreatedAt);
    await AddAsync(student);

    string? storedId = await ScalarAsync<string>("SELECT HEX(id) FROM users");

    storedId.Should().Be(student.Id.ToString("N").ToUpperInvariant());
  }

  [Fact]
  public async Task should_declare_the_identity_column_as_binary_16()
  {
    IReadOnlyList<(string Table, string DataType, long Length)> columns = await QueryAsync(
        """
        SELECT table_name, data_type, character_maximum_length
        FROM information_schema.columns
        WHERE table_schema = DATABASE() AND column_name = 'id'
        """,
        reader => (reader.GetString(0), reader.GetString(1), reader.GetInt64(2)));

    columns.Should().HaveCount(3);
    columns.Should().OnlyContain(column => column.DataType == "binary" && column.Length == 16);
  }

  [Fact]
  public async Task should_keep_the_creation_order_when_sorting_by_identity()
  {
    long baseMilliseconds = CreatedAt.ToUnixTimeMilliseconds();
    long earlierMilliseconds = baseMilliseconds - (baseMilliseconds % 256) + 255;
    DateTimeOffset earlier = DateTimeOffset.FromUnixTimeMilliseconds(earlierMilliseconds);
    DateTimeOffset later = DateTimeOffset.FromUnixTimeMilliseconds(earlierMilliseconds + 1);

    Student first = Student.Register("Ana", "ana@classbooking.dev", "hash", earlier);
    Student second = Student.Register("Bruno", "bruno@classbooking.dev", "hash", later);
    await AddAsync(first, second);

    IReadOnlyList<string> ordered = await QueryAsync(
        "SELECT HEX(id) FROM users ORDER BY id",
        reader => reader.GetString(0));

    ordered.Should().ContainInOrder(
        first.Id.ToString("N").ToUpperInvariant(),
        second.Id.ToString("N").ToUpperInvariant());
  }

  [Fact]
  public async Task should_store_the_utc_instant_when_the_value_has_an_offset()
  {
    DateTimeOffset createdAtInSaoPaulo = new DateTimeOffset(2026, 3, 2, 9, 0, 0, TimeSpan.FromHours(-3));
    Student student = Student.Register("Ana", "ana@classbooking.dev", "hash", createdAtInSaoPaulo);
    await AddAsync(student);

    string? storedInstant = await ScalarAsync<string>(
        "SELECT DATE_FORMAT(created_at, '%Y-%m-%d %H:%i:%s') FROM users");

    storedInstant.Should().Be("2026-03-02 12:00:00");

    using IServiceScope scope = CreateScope();
    AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    User reloaded = await context.Users.SingleAsync();

    reloaded.CreatedAt.Offset.Should().Be(TimeSpan.Zero);
    reloaded.CreatedAt.Should().Be(createdAtInSaoPaulo);
  }

  [Fact]
  public async Task should_declare_created_at_as_datetime_6()
  {
    string? columnType = await ScalarAsync<string>(
        """
        SELECT column_type FROM information_schema.columns
        WHERE table_schema = DATABASE() AND table_name = 'users' AND column_name = 'created_at'
        """);

    columnType.Should().Be("datetime(6)");
  }

  [Fact]
  public async Task should_not_create_timestamp_columns()
  {
    long timestamps = await ScalarAsync<long>(
        """
        SELECT COUNT(*) FROM information_schema.columns
        WHERE table_schema = DATABASE() AND data_type = 'timestamp'
        """);

    timestamps.Should().Be(0);
  }

  [Fact]
  public async Task should_store_the_role_name()
  {
    await AddAsync(
        Student.Register("Ana", "ana@classbooking.dev", "hash", CreatedAt),
        Teacher.Create("Paulo", "paulo@classbooking.dev", "hash", CreatedAt),
        User.CreateAdmin("Root", "root@classbooking.dev", "hash", CreatedAt));

    IReadOnlyList<string> roles = await QueryAsync(
        "SELECT role FROM users ORDER BY role",
        reader => reader.GetString(0));
    string? columnType = await ScalarAsync<string>(
        """
        SELECT column_type FROM information_schema.columns
        WHERE table_schema = DATABASE() AND table_name = 'users' AND column_name = 'role'
        """);

    roles.Should().BeEquivalentTo(["Admin", "Student", "Teacher"]);
    columnType.Should().Be("varchar(30)");
  }
}
