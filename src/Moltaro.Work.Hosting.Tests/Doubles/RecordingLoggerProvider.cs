using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Moltaro.Work.Hosting.Tests.Doubles;

// A logger provider of a test that waits for one log line: the first of its category at its level. It is itself the
// logger of that category; every other category gets a logger that writes nothing.
internal sealed class RecordingLoggerProvider(string category, LogLevel level) : ILoggerProvider, ILogger
{
    private readonly TaskCompletionSource<LogEntry> _entry = new(TaskCreationOptions.RunContinuationsAsynchronously);

    // Completes with the first line of the category at the level.
    public Task<LogEntry> Entry => _entry.Task;

    public ILogger CreateLogger(string categoryName)
    {
        return categoryName == category ? this : NullLogger.Instance;
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (logLevel != level)
        {
            return;
        }

        var values = new Dictionary<string, object?>();

        if (state is IEnumerable<KeyValuePair<string, object?>> pairs)
        {
            foreach ((string name, object? value) in pairs)
            {
                values[name] = value;
            }
        }

        _entry.TrySetResult(new LogEntry(values, exception));
    }

    public void Dispose()
    {
    }
}
