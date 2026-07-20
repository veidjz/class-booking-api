using Microsoft.Extensions.Logging;

namespace ClassBooking.IntegrationTests.Support;

internal sealed class RecordingLoggerProvider : ILoggerProvider
{
  private readonly Lock _gate = new Lock();
  private readonly List<(LogLevel Level, string Message)> _records = [];

  internal IReadOnlyList<(LogLevel Level, string Message)> Records
  {
    get
    {
      lock (_gate)
      {
        return [.. _records];
      }
    }
  }

  public ILogger CreateLogger(string categoryName) => new RecordingLogger(this);

  public void Dispose()
  {
  }

  private void Record(LogLevel level, string message)
  {
    lock (_gate)
    {
      _records.Add((level, message));
    }
  }

  private sealed class RecordingLogger(RecordingLoggerProvider provider) : ILogger
  {
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        provider.Record(logLevel, formatter(state, exception));
  }
}
