namespace Moltaro.Work.Samples.LogsAndRuns;

// The handler of order-mails: the payload is the mail to send. A mail server that is told the item's id as a message
// id sends a mail once, also when the handler runs twice for the item.
internal sealed class OrderMailHandler : IWorkHandler
{
    private readonly TaskCompletionSource _sent = new(TaskCreationOptions.RunContinuationsAsynchronously);

    // Completes once a mail was sent.
    public Task Sent => _sent.Task;

    public Task<WorkHandlerResultEnum> HandleAsync(WorkItem item, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Sending the mail {item.Payload}, item {item.Id}, attempt {item.Attempt}.");

        _sent.TrySetResult();

        return Task.FromResult(WorkHandlerResultEnum.Completed);
    }
}
