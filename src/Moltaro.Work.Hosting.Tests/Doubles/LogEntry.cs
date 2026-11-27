namespace Moltaro.Work.Hosting.Tests.Doubles;

// One log line as a logger provider received it: its structured values, and its exception.
internal sealed record LogEntry(IReadOnlyDictionary<string, object?> Values, Exception? Exception);
