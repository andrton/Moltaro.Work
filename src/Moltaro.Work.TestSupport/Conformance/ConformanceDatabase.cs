using System.Data.Common;

namespace Moltaro.Work.TestSupport.Conformance;

// The real database of one run of a provider's conformance suite, derived once by each provider's test project. The run
// creates the database, applies the provider's schema script and drops the database at the end. The tests share it,
// each with queues of its own name, so that no test sees the items of another.
public abstract class ConformanceDatabase
{
    // The lease of every claim of the suite, long enough that no lease ends while a test runs unless the test moves the
    // database's clock.
    public static readonly TimeSpan Lease = TimeSpan.FromSeconds(30);

    // A new store of the provider over the run's database, as another process would create it.
    public abstract IWorkStore CreateStore();

    // An open connection to the run's database, for a transaction of the caller. The caller disposes it.
    public abstract Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken);

    // Moves the database's clock forward for one queue, so that a test never waits for a due time, a lease end or a
    // retention: one statement shifts every time of the queue's items back by "by".
    public abstract Task MoveClockAsync(WorkQueue queue, TimeSpan by, CancellationToken cancellationToken);

    // A queue of a name of its own, so that a test sees only its own items.
    public static WorkQueue CreateQueue(WorkShapeEnum shape, WorkQueueOptions? options = null)
    {
        return new WorkQueue("conformance-" + Guid.NewGuid().ToString("N"), shape, options);
    }

    // Enqueues an item in a transaction of its own and commits it, so that the item is waiting once this returns.
    public async Task<WorkEnqueueResult> EnqueueCommittedAsync(
        IWorkStore store,
        WorkQueue queue,
        string key,
        string? scope,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await OpenConnectionAsync(cancellationToken);
        await using DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        WorkEnqueueResult result = await store.EnqueueAsync(
            transaction,
            queue,
            key,
            scope,
            payload: null,
            dueAt: null,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return result;
    }
}
