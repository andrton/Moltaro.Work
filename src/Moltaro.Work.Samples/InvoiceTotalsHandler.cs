namespace Moltaro.Work.Samples;

// The handler of a coalescing queue: the key names an invoice, and the handler reads the invoice's current state and
// recomputes its totals. Doing that twice does no harm, which is what makes the handler idempotent.
internal sealed class InvoiceTotalsHandler : IWorkHandler
{
    public Task<WorkHandlerResultEnum> HandleAsync(WorkItem item, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Recomputing the totals of invoice {item.Key}, attempt {item.Attempt}.");

        return Task.FromResult(WorkHandlerResultEnum.Completed);
    }
}
