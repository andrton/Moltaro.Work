using System.Data.Common;

namespace Moltaro.Work;

/// <summary>
/// A transaction of the application joined by <see cref="WorkClient.Join"/>: items are enqueued in it, and it is
/// committed through the library so that the workers of its queues wake.
/// </summary>
/// <remarks>
/// <para>
/// An item enqueued here is written through the transaction's connection, inside the transaction, and exists only if
/// the transaction commits. The library opens no transaction of its own around it.
/// </para>
/// <para>
/// A <see cref="WorkTransaction"/> is used by one caller at a time, as its <see cref="DbTransaction"/> is: the caller
/// awaits each call before the next. The <see cref="DbTransaction"/> stays the application's: a rollback is made on
/// it, and the application disposes it.
/// </para>
/// <para>
/// An argument error and <see cref="InvalidOperationException"/> for a misuse are thrown by the call, before anything
/// happens; a failure of the operation is reported by the returned task.
/// </para>
/// </remarks>
public sealed class WorkTransaction
{
    private readonly IWorkStore _store;

    private readonly DbTransaction _transaction;

    // The names of the queues that an enqueue of this transaction reached, whose signals CommitAsync sets once each.
    private readonly HashSet<string> _enqueuedQueues = new(StringComparer.Ordinal);

    // Set once CommitAsync was called, whatever its outcome: the transaction takes no more enqueues and no second commit.
    private bool _commitCalled;

    // Set once the task of an enqueue faulted or was cancelled: the database may have undone that statement alone, so a
    // commit would keep the application's write without its item.
    private bool _enqueueFailed;

    internal WorkTransaction(IWorkStore store, DbTransaction transaction)
    {
        _store = store;
        _transaction = transaction;
    }

    /// <summary>
    /// Enqueues an item in the transaction: it exists only if the transaction commits.
    /// </summary>
    /// <param name="queue">The queue of the item.</param>
    /// <param name="key">
    /// The key of the item. Required for a coalescing queue, where it names the object that needs attention; optional
    /// for a log or run queue, where it makes the enqueue idempotent. 1 to 200 characters, no control character and no
    /// unpaired surrogate, not ending with a space; it compares exactly, case included.
    /// </param>
    /// <param name="scope">
    /// The scope of the item, only in a queue that is <see cref="WorkQueue.Scoped"/>: an item is not claimed while
    /// another item of its scope is held. The rule of a key applies.
    /// </param>
    /// <param name="payload">
    /// The data of a log or run item, a string that the application serializes, usually JSON, stored as text. A
    /// coalescing item has none. It contains no U+0000 and no unpaired surrogate, so that every provider stores it.
    /// </param>
    /// <param name="dueAt">
    /// The earliest time the item may be claimed, compared with the database's clock and stored in UTC to the
    /// microsecond, truncated. <see langword="null"/>, or an instant that the database's clock has already passed,
    /// means at once.
    /// </param>
    /// <param name="cancellationToken">
    /// Ends the wait for the database. A token that is already cancelled gives a cancelled task, and nothing is
    /// written.
    /// </param>
    /// <returns>
    /// <see cref="WorkEnqueueStatusEnum.Added"/> with the id of the item that became waiting, or
    /// <see cref="WorkEnqueueStatusEnum.Coalesced"/> with the id of the waiting or held item that absorbed the
    /// enqueue, as the queue's <see cref="WorkShapeEnum"/> says.
    /// </returns>
    /// <remarks>
    /// <para>
    /// After its checks the call hands its arguments unchanged to <see cref="IWorkStore.EnqueueAsync"/>, which runs on
    /// the transaction's connection at the transaction's isolation, and remembers the queue, so that
    /// <see cref="CommitAsync"/> sets its signal.
    /// </para>
    /// <para>
    /// The provider neither catches nor retries a database error here: the driver's <see cref="DbException"/>, a
    /// serialization failure or a deadlock for example, is reported by the task, and the transaction is as its
    /// database leaves it: rolled back, aborted, or with that statement alone undone. Each provider documents which
    /// errors its enqueue can raise at which isolation. The caller then rolls the transaction back and retries it, as
    /// for any write. Because the database may have undone that statement alone, once an enqueue's task failed or was
    /// cancelled the transaction takes no more enqueues and no <see cref="CommitAsync"/>, which would keep the
    /// application's write without its item.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown by the call: <paramref name="queue"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown by the call: <see cref="CommitAsync"/> was called, the task of an earlier enqueue of this transaction
    /// faulted or was cancelled, or the transaction has ended, so that its connection is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown by the call, with the parameter's name: for a coalescing queue, <paramref name="key"/> is
    /// <see langword="null"/> or a <paramref name="payload"/> is given; <paramref name="key"/> or
    /// <paramref name="scope"/> breaks the rule of a key; a <paramref name="scope"/> is given for a queue that is not
    /// <see cref="WorkQueue.Scoped"/>; <paramref name="payload"/> breaks its rule. The checks run in this order. After
    /// them the provider refuses a transaction whose connection is not one of its database, with the
    /// <see cref="ArgumentException.ParamName"/> <c>transaction</c>. An argument error writes nothing and leaves the
    /// transaction open for enqueues and the commit.
    /// </exception>
    /// <exception cref="DbException">Reported by the task: the database failed; the driver's exception.</exception>
    /// <exception cref="OperationCanceledException">
    /// Reported by the task: <paramref name="cancellationToken"/> was cancelled.
    /// </exception>
    public Task<WorkEnqueueResult> EnqueueAsync(
        WorkQueue queue,
        string? key = null,
        string? scope = null,
        string? payload = null,
        DateTimeOffset? dueAt = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queue);

        if (_commitCalled)
        {
            throw new InvalidOperationException("CommitAsync was called: the transaction takes no more enqueues.");
        }

        if (_enqueueFailed)
        {
            throw new InvalidOperationException(
                "An earlier enqueue of this transaction failed: roll the transaction back and retry it.");
        }

        if (_transaction.Connection is null)
        {
            throw new InvalidOperationException("The transaction has ended.");
        }

        if (queue.Shape == WorkShapeEnum.Coalescing)
        {
            if (key is null)
            {
                throw new ArgumentException("An item of a coalescing queue has a key.", nameof(key));
            }

            if (payload is not null)
            {
                throw new ArgumentException("An item of a coalescing queue carries no payload.", nameof(payload));
            }
        }

        if (key is not null)
        {
            WorkChecks.CheckKeyOrScope(key, nameof(key));
        }

        if (scope is not null)
        {
            WorkChecks.CheckKeyOrScope(scope, nameof(scope));

            if (!queue.Scoped)
            {
                throw new ArgumentException($"The queue {queue.Name} is not scoped: its items carry no scope.", nameof(scope));
            }
        }

        if (payload is not null)
        {
            WorkChecks.CheckPayload(payload, nameof(payload));
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<WorkEnqueueResult>(cancellationToken);
        }

        // What the store throws by the call is an argument error, such as its ArgumentException for a connection of
        // another database, and wrote nothing: like the core's own argument errors, it leaves the transaction open.
        // Only a task that faulted or was cancelled closes it.
        Task<WorkEnqueueResult> enqueue = _store.EnqueueAsync(_transaction, queue, key, scope, payload, dueAt, cancellationToken);

        return RememberAsync(enqueue, queue);
    }

    /// <summary>
    /// Commits the transaction and then sets the signal of every queue that it enqueued into. Not implemented yet.
    /// </summary>
    /// <param name="cancellationToken">
    /// Ends the wait for the commit. A token that is already cancelled gives a cancelled task, and nothing is
    /// committed. Once the transaction committed, the task completes even if the token is cancelled, because the sets
    /// of the signals do not take it.
    /// </param>
    /// <returns>A task that completes once the transaction committed and the signals were set.</returns>
    /// <remarks>
    /// <para>
    /// Only after the commit succeeded does it set the signal of each queue enqueued into, once each, on the queue's
    /// key in the group <c>moltaro.work</c>. An exception of the commit propagates, and nothing is set. Once committed
    /// it reports nothing about the sets: a set that is lost or fails is covered by the workers, which do a full pass
    /// after a reconnect, and by their idle bound.
    /// </para>
    /// <para>
    /// A rollback is the application's, on its <see cref="DbTransaction"/>. The call counts once, whatever its
    /// outcome: a second call is refused.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown by the call: it was called before, or the task of an enqueue of this transaction faulted or was
    /// cancelled.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown by the call after its checks: committing through the library is not implemented yet.
    /// </exception>
    /// <exception cref="DbException">Reported by the task: the commit failed; the driver's exception.</exception>
    /// <exception cref="OperationCanceledException">
    /// Reported by the task: <paramref name="cancellationToken"/> was cancelled before the transaction committed.
    /// </exception>
    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_commitCalled)
        {
            throw new InvalidOperationException("CommitAsync was called before.");
        }

        if (_enqueueFailed)
        {
            throw new InvalidOperationException(
                "An enqueue of this transaction failed: roll the transaction back and retry it.");
        }

        _commitCalled = true;

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        throw new NotSupportedException("Committing through the library is not implemented yet.");
    }

    // Awaits the store's enqueue; a success remembers the queue for CommitAsync, a failure or a cancellation closes the
    // transaction to further enqueues and to the commit.
    private async Task<WorkEnqueueResult> RememberAsync(Task<WorkEnqueueResult> enqueue, WorkQueue queue)
    {
        WorkEnqueueResult result;

        try
        {
            result = await enqueue.ConfigureAwait(false);
        }
        catch
        {
            _enqueueFailed = true;
            throw;
        }

        _enqueuedQueues.Add(queue.Name);

        return result;
    }
}
