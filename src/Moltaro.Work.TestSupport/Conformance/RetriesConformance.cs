namespace Moltaro.Work.TestSupport.Conformance;

// Retries, dead items and requeue (rules 7 and 8 of concept section 3): a retried item is due again after its delay, a
// given-up item is dead until a requeue, and that state lives in the item, not in a store.
public abstract class RetriesConformance
{
    private readonly ConformanceDatabase _database;

    protected RetriesConformance(ConformanceDatabase database)
    {
        _database = database;
    }

    [Fact(Skip = SkipReason.NotImplemented)]
    public async Task A_retried_item_is_due_again_after_its_delay_as_its_next_attempt()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        WorkQueue queue = ConformanceDatabase.CreateQueue(WorkShapeEnum.Coalescing);
        IWorkStore store = _database.CreateStore();
        await _database.EnqueueCommittedAsync(store, queue, "invoice-7", null, cancellationToken);
        WorkClaim first = Assert.Single(await store.ClaimAsync(queue, 1, ConformanceDatabase.Lease, cancellationToken));

        Assert.True(await store.RetryAsync(first, TimeSpan.FromSeconds(10), "The invoice was locked.", cancellationToken));
        Assert.Empty(await store.ClaimAsync(queue, 1, ConformanceDatabase.Lease, cancellationToken));

        await _database.MoveClockAsync(queue, TimeSpan.FromSeconds(10), cancellationToken);
        WorkClaim second = Assert.Single(await store.ClaimAsync(queue, 1, ConformanceDatabase.Lease, cancellationToken));

        Assert.Equal(first.Item.Id, second.Item.Id);
        Assert.Equal(2, second.Item.Attempt);
    }

    [Fact(Skip = SkipReason.NotImplemented)]
    public async Task A_given_up_item_is_dead_until_a_requeue_makes_it_waiting_with_its_attempts_from_zero()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        WorkQueue queue = ConformanceDatabase.CreateQueue(WorkShapeEnum.Coalescing);
        IWorkStore store = _database.CreateStore();
        await _database.EnqueueCommittedAsync(store, queue, "invoice-7", null, cancellationToken);
        WorkClaim given = Assert.Single(await store.ClaimAsync(queue, 1, ConformanceDatabase.Lease, cancellationToken));

        Assert.Equal(WorkRequeueStatusEnum.NotDead, await store.RequeueAsync(queue, given.Item.Id, cancellationToken));
        Assert.True(await store.GiveUpAsync(given, "The invoice is gone.", cancellationToken));

        // A dead item is not claimed, however late it is.
        await _database.MoveClockAsync(queue, TimeSpan.FromHours(1), cancellationToken);
        Assert.Empty(await store.ClaimAsync(queue, 1, ConformanceDatabase.Lease, cancellationToken));

        Assert.Equal(WorkRequeueStatusEnum.Requeued, await store.RequeueAsync(queue, given.Item.Id, cancellationToken));
        WorkClaim requeued = Assert.Single(await store.ClaimAsync(queue, 1, ConformanceDatabase.Lease, cancellationToken));

        Assert.Equal(given.Item.Id, requeued.Item.Id);
        Assert.Equal(1, requeued.Item.Attempt);
    }

    // A new store stands for a process that started after the first one stopped.
    [Fact(Skip = SkipReason.NotImplemented)]
    public async Task A_new_store_continues_the_attempts_and_the_delay_of_an_item()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        WorkQueue queue = ConformanceDatabase.CreateQueue(WorkShapeEnum.Coalescing);
        IWorkStore before = _database.CreateStore();
        await _database.EnqueueCommittedAsync(before, queue, "invoice-7", null, cancellationToken);
        WorkClaim first = Assert.Single(await before.ClaimAsync(queue, 1, ConformanceDatabase.Lease, cancellationToken));
        Assert.True(await before.RetryAsync(first, TimeSpan.FromSeconds(10), "The invoice was locked.", cancellationToken));

        IWorkStore after = _database.CreateStore();
        Assert.Empty(await after.ClaimAsync(queue, 1, ConformanceDatabase.Lease, cancellationToken));

        await _database.MoveClockAsync(queue, TimeSpan.FromSeconds(10), cancellationToken);
        WorkClaim second = Assert.Single(await after.ClaimAsync(queue, 1, ConformanceDatabase.Lease, cancellationToken));

        Assert.Equal(first.Item.Id, second.Item.Id);
        Assert.Equal(2, second.Item.Attempt);
    }
}
