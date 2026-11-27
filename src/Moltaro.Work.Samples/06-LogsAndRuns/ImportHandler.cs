namespace Moltaro.Work.Samples.LogsAndRuns;

// The handler of nightly-imports: the payload names the file to import, and the key names the night, so that one
// import per night is active at a time.
internal sealed class ImportHandler : IWorkHandler
{
    private readonly TaskCompletionSource _imported = new(TaskCreationOptions.RunContinuationsAsynchronously);

    // Completes once an import has run.
    public Task Imported => _imported.Task;

    public Task<WorkHandlerResultEnum> HandleAsync(WorkItem item, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Importing {item.Payload} for {item.Key}, item {item.Id}, attempt {item.Attempt}.");

        _imported.TrySetResult();

        return Task.FromResult(WorkHandlerResultEnum.Completed);
    }
}
