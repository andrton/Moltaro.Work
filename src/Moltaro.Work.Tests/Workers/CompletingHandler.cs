namespace Moltaro.Work.Tests.Workers;

// A handler for the checks of a worker, which never run it.
internal sealed class CompletingHandler : IWorkHandler
{
    public Task<WorkHandlerResultEnum> HandleAsync(WorkItem item, CancellationToken cancellationToken)
    {
        return Task.FromResult(WorkHandlerResultEnum.Completed);
    }
}
