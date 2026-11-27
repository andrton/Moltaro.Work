namespace Moltaro.Work.Samples.Scopes;

// The handler of customer-syncs. To show what runs beside what, the first change of customer-1 waits until a change of
// customer-2 has started: that can only happen beside it, because the worker never claims the second change of
// customer-1 while the first is held.
internal sealed class CustomerSyncHandler(int itemCount) : IWorkHandler
{
    private readonly TaskCompletionSource _otherCustomerStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly TaskCompletionSource _allHandled = new(TaskCreationOptions.RunContinuationsAsynchronously);

    // Changed only by Interlocked, from both lanes.
    private int _handled;

    // Completes once every item the sample enqueued was handled.
    public Task AllHandled => _allHandled.Task;

    public async Task<WorkHandlerResultEnum> HandleAsync(WorkItem item, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Started item {item.Id} of {item.Scope}: {item.Payload}.");

        if (item.Scope == "customer-2")
        {
            _otherCustomerStarted.TrySetResult();
        }
        else if (!_otherCustomerStarted.Task.IsCompleted)
        {
            await _otherCustomerStarted.Task.WaitAsync(cancellationToken);
        }

        Console.WriteLine($"Ended item {item.Id} of {item.Scope}.");

        if (Interlocked.Increment(ref _handled) == itemCount)
        {
            _allHandled.TrySetResult();
        }

        return WorkHandlerResultEnum.Completed;
    }
}
