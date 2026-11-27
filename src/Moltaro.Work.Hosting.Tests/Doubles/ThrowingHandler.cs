namespace Moltaro.Work.Hosting.Tests.Doubles;

// A handler whose every attempt fails with Failure.
internal sealed class ThrowingHandler : IWorkHandler
{
    public static readonly InvalidOperationException Failure = new("The balance is locked.");

    public Task<WorkHandlerResultEnum> HandleAsync(WorkItem item, CancellationToken cancellationToken)
    {
        throw Failure;
    }
}
