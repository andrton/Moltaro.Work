namespace Moltaro.Work.TestSupport.Conformance;

// Retention (rule 11 of concept section 3): expired items are deleted in bounded batches, and never an item that is
// not finished or dead.
public abstract class RetentionConformance
{
    private static readonly TimeSpan Retention = TimeSpan.FromHours(1);

    private readonly ConformanceDatabase _database;

    protected RetentionConformance(ConformanceDatabase database)
    {
        _database = database;
    }

    [Fact(Skip = SkipReason.NotImplemented)]
    public async Task A_delete_removes_expired_items_in_batches_of_at_most_its_count()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        WorkQueue queue = ConformanceDatabase.CreateQueue(WorkShapeEnum.Coalescing, new WorkQueueOptions { Retention = Retention });
        IWorkStore store = _database.CreateStore();

        for (int invoice = 1; invoice <= 3; invoice++)
        {
            await _database.EnqueueCommittedAsync(store, queue, $"invoice-{invoice}", null, cancellationToken);
        }

        foreach (WorkClaim claim in await store.ClaimAsync(queue, 3, ConformanceDatabase.Lease, cancellationToken))
        {
            Assert.True(await store.GiveUpAsync(claim, "The invoice is gone.", cancellationToken));
        }

        Assert.Equal(0, await store.DeleteExpiredAsync(queue, 2, cancellationToken));

        await _database.MoveClockAsync(queue, Retention + TimeSpan.FromSeconds(1), cancellationToken);

        Assert.Equal(2, await store.DeleteExpiredAsync(queue, 2, cancellationToken));
        Assert.Equal(1, await store.DeleteExpiredAsync(queue, 2, cancellationToken));
        Assert.Equal(0, await store.DeleteExpiredAsync(queue, 2, cancellationToken));
    }

    [Fact(Skip = SkipReason.NotImplemented)]
    public async Task A_delete_never_removes_an_item_that_waits_or_is_held()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        WorkQueue queue = ConformanceDatabase.CreateQueue(WorkShapeEnum.Coalescing, new WorkQueueOptions { Retention = Retention });
        IWorkStore store = _database.CreateStore();
        await _database.EnqueueCommittedAsync(store, queue, "invoice-1", null, cancellationToken);
        await _database.EnqueueCommittedAsync(store, queue, "invoice-2", null, cancellationToken);
        Assert.Single(await store.ClaimAsync(queue, 1, ConformanceDatabase.Lease, cancellationToken));

        await _database.MoveClockAsync(queue, Retention * 2, cancellationToken);

        Assert.Equal(0, await store.DeleteExpiredAsync(queue, 10, cancellationToken));

        // Both are still there: the waiting one, and the held one, whose lease has ended.
        Assert.Equal(2, (await store.ClaimAsync(queue, 10, ConformanceDatabase.Lease, cancellationToken)).Count);
    }
}
