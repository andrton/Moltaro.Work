namespace Moltaro.Work.Hosting.Tests.Doubles;

// A handler that records its item and waits until its token is cancelled, as a handler that is busy when the
// application stops; it records that it ended before it returns.
internal sealed class WaitingHandler(IServiceProvider services, HandledItems handled) : IWorkHandler
{
    public async Task<WorkHandlerResultEnum> HandleAsync(WorkItem item, CancellationToken cancellationToken)
    {
        handled.Start(new HandledItem(item, services, cancellationToken));

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
        finally
        {
            handled.End(item.Id);
        }

        return WorkHandlerResultEnum.Completed;
    }
}
