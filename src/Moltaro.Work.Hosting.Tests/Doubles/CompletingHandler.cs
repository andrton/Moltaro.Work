namespace Moltaro.Work.Hosting.Tests.Doubles;

// A handler that records each item with the provider of its scope, and completes it.
internal sealed class CompletingHandler(IServiceProvider services, HandledItems handled) : IWorkHandler
{
    public Task<WorkHandlerResultEnum> HandleAsync(WorkItem item, CancellationToken cancellationToken)
    {
        handled.Start(new HandledItem(item, services, cancellationToken));
        handled.End(item.Id);

        return Task.FromResult(WorkHandlerResultEnum.Completed);
    }
}
