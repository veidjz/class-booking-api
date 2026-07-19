using ClassBooking.Domain.Users;
using ClassBooking.Infrastructure.Persistence;
using ClassBooking.IntegrationTests.Persistence.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClassBooking.IntegrationTests.Persistence;

[Collection(nameof(DatabaseCollection))]
public sealed class VersionTokenTests(ContainersFixture fixture) : DatabaseTestBase(fixture)
{
  private static readonly DateTimeOffset CreatedAt = new DateTimeOffset(2026, 3, 2, 12, 0, 0, TimeSpan.Zero);

  [Fact]
  public async Task should_insert_new_accounts_with_version_zero()
  {
    await AddAsync(Student.Register("Ana", "ana@classbooking.dev", "hash", CreatedAt));

    long version = await ScalarAsync<long>("SELECT version FROM users");

    version.Should().Be(0);
  }

  [Fact]
  public async Task should_bump_the_base_version_when_only_a_subtype_column_changes()
  {
    Teacher teacher = Teacher.Create("Paulo", "paulo@classbooking.dev", "hash", CreatedAt);
    await AddAsync(teacher);

    using (IServiceScope scope = CreateScope())
    {
      AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
      Teacher tracked = await context.Users.OfType<Teacher>().SingleAsync();
      context.Entry(tracked).Property(entity => entity.NoShowCount).CurrentValue = 1;
      await context.SaveChangesAsync();
    }

    long version = await ScalarAsync<long>("SELECT version FROM users");
    long noShowCount = await ScalarAsync<long>("SELECT no_show_count FROM teachers");

    version.Should().Be(1);
    noShowCount.Should().Be(1);
  }

  [Fact]
  public async Task should_throw_when_two_contexts_update_the_same_account()
  {
    await AddAsync(User.CreateAdmin("Root", "root@classbooking.dev", "hash", CreatedAt));

    using IServiceScope firstScope = CreateScope();
    using IServiceScope secondScope = CreateScope();
    AppDbContext firstContext = firstScope.ServiceProvider.GetRequiredService<AppDbContext>();
    AppDbContext secondContext = secondScope.ServiceProvider.GetRequiredService<AppDbContext>();

    User firstUser = await firstContext.Users.SingleAsync();
    User secondUser = await secondContext.Users.SingleAsync();

    firstUser.Deactivate(CreatedAt);
    secondUser.Deactivate(CreatedAt);

    await firstContext.SaveChangesAsync();
    Func<Task> secondSave = () => secondContext.SaveChangesAsync();

    await secondSave.Should().ThrowAsync<DbUpdateConcurrencyException>();
  }
}
