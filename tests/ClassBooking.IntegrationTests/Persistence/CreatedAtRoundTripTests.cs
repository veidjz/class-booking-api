using ClassBooking.Domain.Users;
using ClassBooking.Infrastructure.Persistence;
using ClassBooking.Infrastructure.Time;
using ClassBooking.IntegrationTests.Persistence.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace ClassBooking.IntegrationTests.Persistence;

[Collection(nameof(DatabaseCollection))]
public sealed class CreatedAtRoundTripTests(ContainersFixture fixture) : DatabaseTestBase(fixture)
{
  [Fact]
  public async Task should_round_trip_created_at_exactly()
  {
    FakeTimeProvider fake = new FakeTimeProvider(
        new DateTimeOffset(2026, 3, 2, 12, 0, 0, TimeSpan.Zero).AddTicks(1234567));
    MicrosecondTimeProvider provider = new MicrosecondTimeProvider(fake);
    DateTimeOffset now = provider.GetUtcNow();

    Student student = Student.Register("Ana", "ana@classbooking.dev", "hash", now);
    await AddAsync(student);

    using IServiceScope scope = CreateScope();
    AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    User reloaded = await context.Users.SingleAsync();

    reloaded.CreatedAt.Should().Be(now);
  }
}
