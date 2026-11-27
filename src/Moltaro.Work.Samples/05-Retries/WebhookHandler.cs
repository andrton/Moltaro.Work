namespace Moltaro.Work.Samples.Retries;

// The handler of webhook-deliveries. While the receiver is down, every attempt throws: an exception is a failure, kept
// as the item's reason, and after the queue's attempt limit the item is dead. Once the receiver is up, a delivery
// completes.
internal sealed class WebhookHandler : IWorkHandler
{
    private readonly TaskCompletionSource _lastAttemptFailed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly TaskCompletionSource<WorkItem> _delivered = new(TaskCreationOptions.RunContinuationsAsynchronously);

    // Set by the sample once the receiver is fixed, while no worker runs this handler.
    public bool ReceiverIsUp { get; set; }

    // Completes when the attempt at the queue's attempt limit is failing.
    public Task LastAttemptFailed => _lastAttemptFailed.Task;

    // Completes with the item once a delivery completed it.
    public Task<WorkItem> Delivered => _delivered.Task;

    public Task<WorkHandlerResultEnum> HandleAsync(WorkItem item, CancellationToken cancellationToken)
    {
        if (!ReceiverIsUp)
        {
            Console.WriteLine($"Delivering {item.Payload}, attempt {item.Attempt} of {item.Queue.AttemptLimit}: the receiver is down.");

            if (item.Attempt == item.Queue.AttemptLimit)
            {
                _lastAttemptFailed.TrySetResult();
            }

            throw new HttpRequestException("The receiver answered 503 Service Unavailable.");
        }

        Console.WriteLine($"Delivering {item.Payload}, attempt {item.Attempt}: delivered.");

        _delivered.TrySetResult(item);

        return Task.FromResult(WorkHandlerResultEnum.Completed);
    }
}
