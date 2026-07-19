using ClassBooking.Infrastructure.Time;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;

namespace ClassBooking.IntegrationTests.Time;

public sealed class MicrosecondTimeProviderTests
{
  private static readonly DateTimeOffset Instant =
      new DateTimeOffset(2026, 3, 2, 12, 0, 0, TimeSpan.Zero).AddTicks(1234567);

  [Fact]
  public void should_truncate_the_current_instant_to_microseconds()
  {
    FakeTimeProvider fake = new FakeTimeProvider(Instant);
    MicrosecondTimeProvider provider = new MicrosecondTimeProvider(fake);

    DateTimeOffset now = provider.GetUtcNow();

    now.Ticks.Should().Be(Instant.Ticks - (Instant.Ticks % 10));
    (now.Ticks % 10).Should().Be(0);
  }

  [Fact]
  public void should_delegate_the_timestamp_members()
  {
    FakeTimeProvider fake = new FakeTimeProvider(Instant);
    MicrosecondTimeProvider provider = new MicrosecondTimeProvider(fake);

    long start = provider.GetTimestamp();
    fake.Advance(TimeSpan.FromSeconds(2));
    long end = provider.GetTimestamp();

    provider.TimestampFrequency.Should().Be(fake.TimestampFrequency);
    provider.LocalTimeZone.Should().Be(fake.LocalTimeZone);
    provider.GetElapsedTime(start, end).Should().Be(TimeSpan.FromSeconds(2));
  }
}
