namespace Moltaro.Work.Samples.Retries;

// The handler of invoice-exports. The export target is busy at the first attempt, so the handler asks for a retry
// instead of failing: the item is due again after the queue's backoff, and the next attempt completes it.
internal sealed class InvoiceExportHandler : IWorkHandler
{
    private readonly TaskCompletionSource<WorkItem> _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    // Completes with the item once an attempt has completed it.
    public Task<WorkItem> Completed => _completed.Task;

    public Task<WorkHandlerResultEnum> HandleAsync(WorkItem item, CancellationToken cancellationToken)
    {
        if (item.Attempt == 1)
        {
            Console.WriteLine($"Exporting {item.Payload}, attempt 1: the target is busy, so the handler asks for a retry.");

            return Task.FromResult(WorkHandlerResultEnum.Retry);
        }

        Console.WriteLine($"Exporting {item.Payload}, attempt {item.Attempt}: exported.");

        _completed.TrySetResult(item);

        return Task.FromResult(WorkHandlerResultEnum.Completed);
    }
}
