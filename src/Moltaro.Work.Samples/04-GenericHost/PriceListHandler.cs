using Microsoft.Extensions.Hosting;

namespace Moltaro.Work.Samples.GenericHost;

// The handler of price-lists, resolved from the container in a scope of its own for every item, so it takes the
// application's services in its constructor. It recomputes the price list, and then, because the sample ends here,
// stops the application.
internal sealed class PriceListHandler(IHostApplicationLifetime lifetime) : IWorkHandler
{
    public Task<WorkHandlerResultEnum> HandleAsync(WorkItem item, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Recomputing {item.Key}, attempt {item.Attempt}; then the application stops.");

        lifetime.StopApplication();

        return Task.FromResult(WorkHandlerResultEnum.Completed);
    }
}
