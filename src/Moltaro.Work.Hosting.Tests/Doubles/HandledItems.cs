using System.Threading.Channels;

namespace Moltaro.Work.Hosting.Tests.Doubles;

// What the handlers of one container saw, registered there as a singleton: every item a handler started, in order,
// and the ids of those whose handler has ended. A test waits for the next started item; it never polls.
internal sealed class HandledItems
{
    private readonly Channel<HandledItem> _started = Channel.CreateUnbounded<HandledItem>();

    // Guards _ended, which the lanes of a worker and the test reach at once.
    private readonly Lock _lock = new();

    private readonly HashSet<long> _ended = [];

    public void Start(HandledItem item)
    {
        _started.Writer.TryWrite(item);
    }

    public void End(long itemId)
    {
        lock (_lock)
        {
            _ended.Add(itemId);
        }
    }

    public ValueTask<HandledItem> NextStartedAsync(CancellationToken cancellationToken)
    {
        return _started.Reader.ReadAsync(cancellationToken);
    }

    public bool HasEnded(long itemId)
    {
        lock (_lock)
        {
            return _ended.Contains(itemId);
        }
    }
}
