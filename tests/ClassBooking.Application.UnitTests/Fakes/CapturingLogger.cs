using Microsoft.Extensions.Logging;

namespace ClassBooking.Application.UnitTests.Fakes;

internal sealed class CapturingLogger<T> : ILogger<T>
{
  private readonly List<LogEntry> _entries = [];
  private readonly List<object> _scopes = [];

  internal IReadOnlyList<LogEntry> Entries => _entries;

  internal IReadOnlyList<object> Scopes => _scopes;

  public IDisposable BeginScope<TState>(TState state)
      where TState : notnull
  {
    _scopes.Add(state);
    return new NoopScope();
  }

  public bool IsEnabled(LogLevel logLevel) => true;

  public void Log<TState>(
      LogLevel logLevel,
      EventId eventId,
      TState state,
      Exception? exception,
      Func<TState, Exception?, string> formatter)
  {
    var properties = state as IReadOnlyList<KeyValuePair<string, object?>> ?? [];
    _entries.Add(new LogEntry(logLevel, formatter(state, exception), properties.ToDictionary()));
  }

  internal sealed record LogEntry(LogLevel Level, string Message, IReadOnlyDictionary<string, object?> Properties);

  private sealed class NoopScope : IDisposable
  {
    public void Dispose()
    {
    }
  }
}
