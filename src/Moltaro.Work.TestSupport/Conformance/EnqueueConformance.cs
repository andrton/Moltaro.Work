using System.Data.Common;

namespace Moltaro.Work.TestSupport.Conformance;

// The enqueue in the caller's transaction (rule 1 of concept section 3): an item exists only if that transaction
// commits.
public abstract class EnqueueConformance
{
    private readonly ConformanceDatabase _database;

    protected EnqueueConformance(ConformanceDatabase database)
    {
        _database = database;
    }

    [Fact(Skip = SkipReason.NotImplemented)]
    public async Task A_rolled_back_enqueue_leaves_nothing_to_claim()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        WorkQueue queue = ConformanceDatabase.CreateQueue(WorkShapeEnum.Coalescing);
        IWorkStore store = _database.CreateStore();

        await using (DbConnection connection = await _database.OpenConnectionAsync(cancellationToken))
        {
            await using DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

            await store.EnqueueAsync(transaction, queue, "invoice-7", null, null, null, cancellationToken);
            await transaction.RollbackAsync(cancellationToken);
        }

        Assert.Empty(await store.ClaimAsync(queue, 10, ConformanceDatabase.Lease, cancellationToken));
    }

    [Fact(Skip = SkipReason.NotImplemented)]
    public async Task A_committed_enqueue_is_claimable()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        WorkQueue queue = ConformanceDatabase.CreateQueue(WorkShapeEnum.Coalescing);
        IWorkStore store = _database.CreateStore();

        WorkEnqueueResult enqueued = await _database.EnqueueCommittedAsync(store, queue, "invoice-7", null, cancellationToken);

        Assert.Equal(WorkEnqueueStatusEnum.Added, enqueued.Status);
        WorkClaim claim = Assert.Single(await store.ClaimAsync(queue, 10, ConformanceDatabase.Lease, cancellationToken));
        Assert.Equal(enqueued.ItemId, claim.Item.Id);
        Assert.Equal(queue.Name, claim.Item.Queue.Name);
        Assert.Equal("invoice-7", claim.Item.Key);
        Assert.Equal(1, claim.Item.Attempt);
    }
}
