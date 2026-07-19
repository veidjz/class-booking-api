using ClassBooking.Application.Abstractions.Data;
using ClassBooking.Domain.Common;
using ClassBooking.Domain.Users;
using ClassBooking.Infrastructure.Persistence;
using ClassBooking.IntegrationTests.Persistence.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClassBooking.IntegrationTests.Persistence;

[Collection(nameof(DatabaseCollection))]
public sealed class DuplicateEmailTests(ContainersFixture fixture) : DatabaseTestBase(fixture)
{
  private static readonly DateTimeOffset CreatedAt = new DateTimeOffset(2026, 3, 2, 12, 0, 0, TimeSpan.Zero);

  [Fact]
  public Task should_fail_with_email_already_in_use_when_the_email_repeats() =>
      AssertDuplicateAsync("ana@classbooking.dev", "ana@classbooking.dev");

  [Fact]
  public Task should_fail_when_the_email_differs_only_by_case() =>
      AssertDuplicateAsync("ana@classbooking.dev", "ANA@CLASSBOOKING.DEV");

  [Fact]
  public Task should_fail_when_the_email_differs_only_by_accent() =>
      AssertDuplicateAsync("joao@classbooking.dev", "joão@classbooking.dev");

  [Fact]
  public async Task should_rethrow_when_the_violation_is_not_a_duplicate_key()
  {
    Teacher teacher = Teacher.Create("Paulo", "paulo@classbooking.dev", "hash", CreatedAt);
    await AddAsync(teacher);

    using IServiceScope scope = CreateScope();
    AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
    Teacher tracked = await context.Users.OfType<Teacher>().SingleAsync();
    context.Entry(tracked).Property(entity => entity.NoShowCount).CurrentValue = -1;

    Func<Task> save = () => unitOfWork.SaveChangesAsync(default);

    await save.Should().ThrowAsync<DbUpdateException>();
  }

  private async Task AssertDuplicateAsync(string firstEmail, string secondEmail)
  {
    await AddAsync(Student.Register("Ana", firstEmail, "hash", CreatedAt));

    using IServiceScope scope = CreateScope();
    AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
    context.Users.Add(Student.Register("Bruno", secondEmail, "hash", CreatedAt));

    Result result = await unitOfWork.SaveChangesAsync(default);

    result.IsFailure.Should().BeTrue();
    result.Error.Should().Be(UserErrors.EmailAlreadyInUse);
  }
}
