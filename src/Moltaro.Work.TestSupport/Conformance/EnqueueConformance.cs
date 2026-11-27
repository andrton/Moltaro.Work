using System.Data.Common;

namespace Moltaro.Work.TestSupport.Conformance;

// The enqueue in the caller's transaction (rule 1 of concept section 3): an item exists only if that transaction
// commits, as a second enqueue of the same key on a coalescing queue shows.
public abstract class EnqueueConformance
{
    private readonly ConformanceDatabase _database;

    protected EnqueueConformance(ConformanceDatabase database)
    {
        _database = database;
    }

    [Fact(Skip = SkipReason.NotImplemented)]
    public async Task After_a_rolled_back_enqueue_the_same_key_is_added_again()
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

        WorkEnqueueResult again = await _database.EnqueueCommittedAsync(store, queue, "invoice-7", null, cancellationToken);

        Assert.Equal(WorkEnqueueStatusEnum.Added, again.Status);
    }

    [Fact(Skip = SkipReason.NotImplemented)]
    public async Task After_a_committed_enqueue_the_same_key_coalesces_into_its_item()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        WorkQueue queue = ConformanceDatabase.CreateQueue(WorkShapeEnum.Coalescing);
        IWorkStore store = _database.CreateStore();

        WorkEnqueueResult first = await _database.EnqueueCommittedAsync(store, queue, "invoice-7", null, cancellationToken);
        WorkEnqueueResult second = await _database.EnqueueCommittedAsync(store, queue, "invoice-7", null, cancellationToken);

        Assert.Equal(WorkEnqueueStatusEnum.Added, first.Status);
        Assert.Equal(WorkEnqueueStatusEnum.Coalesced, second.Status);
        Assert.Equal(first.ItemId, second.ItemId);
    }
}
