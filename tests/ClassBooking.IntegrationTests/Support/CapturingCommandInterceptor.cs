using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ClassBooking.IntegrationTests.Support;

public sealed class CapturingCommandInterceptor : DbCommandInterceptor
{
  private readonly List<string> _commands = [];

  public IReadOnlyList<string> Commands => _commands;

  public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
      DbCommand command,
      CommandEventData eventData,
      InterceptionResult<DbDataReader> result,
      CancellationToken cancellationToken = default)
  {
    _commands.Add(command.CommandText);

    return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
  }

  public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
      DbCommand command,
      CommandEventData eventData,
      InterceptionResult<int> result,
      CancellationToken cancellationToken = default)
  {
    _commands.Add(command.CommandText);

    return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
  }
}
