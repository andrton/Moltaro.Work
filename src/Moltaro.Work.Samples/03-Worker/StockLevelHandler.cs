namespace Moltaro.Work.Samples.Worker;

// The handler of stock-levels: the key names a product, and the handler recomputes its stock levels from the product's
// current rows, which does no harm when it runs twice. It tells the sample which item it handled.
internal sealed class StockLevelHandler : IWorkHandler
{
    private readonly TaskCompletionSource<WorkItem> _handled = new(TaskCreationOptions.RunContinuationsAsynchronously);

    // Completes with the first item the handler handled.
    public Task<WorkItem> Handled => _handled.Task;

    public Task<WorkHandlerResultEnum> HandleAsync(WorkItem item, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Recomputing the stock levels of {item.Key}, attempt {item.Attempt}.");

        _handled.TrySetResult(item);

        return Task.FromResult(WorkHandlerResultEnum.Completed);
    }
}
