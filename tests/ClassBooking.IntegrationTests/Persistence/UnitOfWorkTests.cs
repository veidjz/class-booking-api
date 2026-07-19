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
public sealed class UnitOfWorkTests(ContainersFixture fixture) : DatabaseTestBase(fixture)
{
  private static readonly DateTimeOffset CreatedAt = new DateTimeOffset(2026, 3, 2, 12, 0, 0, TimeSpan.Zero);

  [Fact]
  public async Task should_see_rows_committed_by_other_connections_inside_the_transaction()
  {
    using IServiceScope readerScope = CreateScope();
    AppDbContext readerContext = readerScope.ServiceProvider.GetRequiredService<AppDbContext>();
    IUnitOfWork readerUnitOfWork = readerScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

    await using IUnitOfWorkTransaction transaction = await readerUnitOfWork.BeginTransactionAsync(default);
    int before = await readerContext.Users.CountAsync();

    await AddAsync(Student.Register("Ana", "ana@classbooking.dev", "hash", CreatedAt));

    int after = await readerContext.Users.CountAsync();

    before.Should().Be(0);
    after.Should().Be(1);
  }

  [Fact]
  public async Task should_not_persist_when_the_transaction_is_disposed_without_commit()
  {
    using (IServiceScope scope = CreateScope())
    {
      AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
      IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

      await using IUnitOfWorkTransaction transaction = await unitOfWork.BeginTransactionAsync(default);
      context.Users.Add(Student.Register("Ana", "ana@classbooking.dev", "hash", CreatedAt));
      await unitOfWork.SaveChangesAsync(default);
    }

    long users = await ScalarAsync<long>("SELECT COUNT(*) FROM users");

    users.Should().Be(0);
  }

  [Fact]
  public async Task should_persist_when_the_transaction_is_committed()
  {
    using (IServiceScope scope = CreateScope())
    {
      AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
      IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

      await using IUnitOfWorkTransaction transaction = await unitOfWork.BeginTransactionAsync(default);
      context.Users.Add(Student.Register("Ana", "ana@classbooking.dev", "hash", CreatedAt));
      await unitOfWork.SaveChangesAsync(default);
      await transaction.CommitAsync(default);
    }

    long users = await ScalarAsync<long>("SELECT COUNT(*) FROM users");

    users.Should().Be(1);
  }

  [Fact]
  public void should_dequeue_domain_events_once()
  {
    using IServiceScope scope = CreateScope();
    AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

    context.Users.Add(Student.Register("Ana", "ana@classbooking.dev", "hash", CreatedAt));

    IReadOnlyCollection<IDomainEvent> first = unitOfWork.DequeueDomainEvents();
    IReadOnlyCollection<IDomainEvent> second = unitOfWork.DequeueDomainEvents();

    first.Should().ContainSingle().Which.Should().BeOfType<StudentRegisteredDomainEvent>();
    second.Should().BeEmpty();
  }
}
