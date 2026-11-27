using System.Data.Common;
using Moltaro.Work.TestSupport.Doubles;

namespace Moltaro.Work.TestSupport.Conformance;

// The enqueue in the caller's transaction (rule 1 of concept section 3): an item exists only if that transaction
// commits, as a second enqueue of the same key on a coalescing queue shows; and only a transaction of the store's own
// database is taken, which every store checks from its contract on.
public abstract class EnqueueConformance
{
    private readonly ConformanceDatabase _database;

    protected EnqueueConformance(ConformanceDatabase database)
    {
        _database = database;
    }

    // A refusal by the call, before any task exists, so that the caller's transaction stays open for its other
    // enqueues and its commit.
    [Fact]
    public void An_enqueue_on_a_transaction_of_another_database_is_refused_by_the_call()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        WorkQueue queue = ConformanceDatabase.CreateQueue(WorkShapeEnum.Coalescing);
        IWorkStore store = _database.CreateStore();
        var transaction = new FakeDbTransaction(new FakeDbConnection());

        Assert.Throws<ArgumentException>(
            "transaction",
            () => { _ = store.EnqueueAsync(transaction, queue, "invoice-7", null, null, null, cancellationToken); });
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
