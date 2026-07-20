namespace ClassBooking.Infrastructure.Time;

internal sealed class MicrosecondTimeProvider(TimeProvider inner) : TimeProvider
{
  public override TimeZoneInfo LocalTimeZone => inner.LocalTimeZone;

  public override long TimestampFrequency => inner.TimestampFrequency;

  public override DateTimeOffset GetUtcNow()
  {
    DateTimeOffset now = inner.GetUtcNow();

    return new DateTimeOffset(now.Ticks - (now.Ticks % 10), now.Offset);
  }

  public override long GetTimestamp() => inner.GetTimestamp();

  public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) =>
      inner.CreateTimer(callback, state, dueTime, period);
}
