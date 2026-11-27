using System.Data.Common;

namespace Moltaro.Work;

/// <summary>
/// The provider contract: the operations of the core on the items of one database, implemented once per database by
/// a provider package.
/// </summary>
/// <remarks>
/// <para>
/// An item is waiting, held, finished or dead. A waiting item may be claimed once its due time is reached; a held item
/// belongs to the holder of its current claim until its lease ends; a finished run and a dead item are kept for the
/// queue's retention. Every time is the database's: due times, lease ends and retention are compared with the
/// database's clock, never with a time of the worker, and no method takes an instant from the caller but the due time
/// of an enqueue.
/// </para>
/// <para>
/// Every method may be called concurrently. The core calls it only with arguments it has checked, so an
/// implementation checks only what the core cannot, such as the type of the transaction's connection
/// (<see cref="ArgumentException"/>). <see cref="EnqueueAsync"/> runs on the connection of the caller's transaction,
/// inside it; every other method runs on a connection and in a transaction of its own, at the isolation the provider
/// names.
/// </para>
/// <para>
/// A token that is already cancelled when a method is called gives a cancelled task, and nothing is written. A
/// cancellation that comes later ends the task cancelled, with one exception: <see cref="RequeueAsync"/> completes
/// with its result once its transaction committed, so that the caller can set the queue's signal.
/// </para>
/// <para>
/// A fenced write, <see cref="CompleteAsync"/>, <see cref="RetryAsync"/>, <see cref="GiveUpAsync"/>,
/// <see cref="ReleaseAsync"/> and the renewals of <see cref="HeartbeatAsync"/>, succeeds only while the claim's fence
/// is the item's current one and the item is held. Otherwise it writes nothing and reports the refusal: a holder whose
/// lease was reclaimed cannot change the item. A write that ends a hold also ends the hold of the item's scope.
/// </para>
/// <para>
/// A failure of the database is reported by the task, as the driver's <see cref="DbException"/>.
/// </para>
/// </remarks>
public interface IWorkStore
{
    /// <summary>
    /// Checks that the database is one the provider can work with: the schema version it needs, and each setting of
    /// the database it names.
    /// </summary>
    /// <param name="cancellationToken">Ends the wait for the database, as the remarks of <see cref="IWorkStore"/> say.</param>
    /// <returns>A task that completes when the database fits.</returns>
    /// <exception cref="InvalidOperationException">
    /// Reported by the task: the schema version found is not the one needed, both named in the message, or a setting
    /// that the provider needs is off, named in the message.
    /// </exception>
    Task CheckDatabaseAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Enqueues an item through the caller's transaction, as the queue's <see cref="WorkShapeEnum"/> says.
    /// </summary>
    /// <param name="transaction">
    /// The caller's open transaction; the statement runs on its connection, inside it, at its isolation.
    /// </param>
    /// <param name="queue">The queue of the item.</param>
    /// <param name="key">The key of the item, or <see langword="null"/> for a log or run item without one.</param>
    /// <param name="scope">The scope of the item, or <see langword="null"/>.</param>
    /// <param name="payload">The payload of a log or run item, stored as text, or <see langword="null"/>.</param>
    /// <param name="dueAt">
    /// The earliest time the item may be claimed; <see langword="null"/>, or an instant the database's clock has
    /// passed, means its now. It is stored in UTC to the microsecond, truncated.
    /// </param>
    /// <param name="cancellationToken">Ends the wait for the database, as the remarks of <see cref="IWorkStore"/> say.</param>
    /// <returns>
    /// <see cref="WorkEnqueueStatusEnum.Added"/> with the id of the item that became waiting: a new item, or a dead
    /// coalescing item revived. <see cref="WorkEnqueueStatusEnum.Coalesced"/> with the id of the waiting or held item
    /// that absorbed the enqueue.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Without a key, or when no item of the queue holds the key, it adds a waiting item due at the later of
    /// <paramref name="dueAt"/> and the database's now. For a coalescing item of that key: while it waits, its due
    /// time becomes the earlier of the two, unless it waits for a retry; while it is held, its mark to run again takes
    /// the earlier of the two; while it is dead, it is waiting again with that due time and its attempts at zero. For
    /// a log or run item whose key an active item has, it writes nothing.
    /// </para>
    /// <para>
    /// It neither catches nor retries a database error: the driver's <see cref="DbException"/> reaches the caller
    /// through the task, and the transaction is as its database leaves it: rolled back, aborted, or with that
    /// statement alone undone. Each provider documents which errors its enqueue can raise at which isolation.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown by the call: the transaction's connection is not one of the provider's database.
    /// </exception>
    Task<WorkEnqueueResult> EnqueueAsync(
        DbTransaction transaction,
        WorkQueue queue,
        string? key,
        string? scope,
        string? payload,
        DateTimeOffset? dueAt,
        CancellationToken cancellationToken);

    /// <summary>
    /// Claims at most <paramref name="count"/> of the queue's claimable items and leases them to the caller.
    /// </summary>
    /// <param name="queue">The queue whose items are claimed.</param>
    /// <param name="count">The most items to claim; at least 1.</param>
    /// <param name="lease">The lease of each claim, counted from the database's now.</param>
    /// <param name="cancellationToken">Ends the wait for the database, as the remarks of <see cref="IWorkStore"/> say.</param>
    /// <returns>The claims; empty when nothing was claimable.</returns>
    /// <remarks>
    /// An item is claimable when it waits and its due time is reached, or when it is held and its lease has ended.
    /// Each claimed item becomes held, with its fence and its attempts one higher, its lease ending at the database's
    /// now plus <paramref name="lease"/>, and its time of claim recorded. Two concurrent claims never return the same
    /// item: an item locked by another transaction is skipped. In a scoped queue at most one item of a scope is
    /// claimed, and none whose scope is held.
    /// </remarks>
    Task<IReadOnlyList<WorkClaim>> ClaimAsync(WorkQueue queue, int count, TimeSpan lease, CancellationToken cancellationToken);

    /// <summary>
    /// Renews the leases of claims: each lease ends at the database's now plus <paramref name="lease"/>.
    /// </summary>
    /// <param name="claims">The claims whose leases are renewed.</param>
    /// <param name="lease">The new lease, counted from the database's now.</param>
    /// <param name="cancellationToken">Ends the wait for the database, as the remarks of <see cref="IWorkStore"/> say.</param>
    /// <returns>
    /// The claims it refused, whose fence is no longer the item's current one; their items are not changed. Empty when
    /// every lease was renewed.
    /// </returns>
    Task<IReadOnlyList<WorkClaim>> HeartbeatAsync(IReadOnlyList<WorkClaim> claims, TimeSpan lease, CancellationToken cancellationToken);

    /// <summary>
    /// Completes a claimed item after its handler succeeded.
    /// </summary>
    /// <param name="claim">The claim of the item.</param>
    /// <param name="cancellationToken">Ends the wait for the database, as the remarks of <see cref="IWorkStore"/> say.</param>
    /// <returns>
    /// <see langword="true"/> when the item was completed; <see langword="false"/> when the claim's fence is not the
    /// item's current one, and nothing was written.
    /// </returns>
    /// <remarks>
    /// A coalescing item is deleted, or, when an enqueue marked it to run again while it was held, is waiting again at
    /// the mark's due time with its attempts at zero. A log item is deleted. A run is finished and kept for the queue's
    /// retention, and no longer holds its key.
    /// </remarks>
    Task<bool> CompleteAsync(WorkClaim claim, CancellationToken cancellationToken);

    /// <summary>
    /// Makes a claimed item waiting again after a delay, for a failed attempt or a requested retry.
    /// </summary>
    /// <param name="claim">The claim of the item.</param>
    /// <param name="delay">The delay, counted from the database's now.</param>
    /// <param name="reason">The reason kept with the item; at most 4000 characters.</param>
    /// <param name="cancellationToken">Ends the wait for the database, as the remarks of <see cref="IWorkStore"/> say.</param>
    /// <returns>
    /// <see langword="true"/> when the item is waiting again; <see langword="false"/> when the claim's fence is not
    /// the item's current one, and nothing was written.
    /// </returns>
    /// <remarks>
    /// The item is due at the database's now plus <paramref name="delay"/> and keeps its attempts. A mark to run again
    /// stays for the next attempt.
    /// </remarks>
    Task<bool> RetryAsync(WorkClaim claim, TimeSpan delay, string reason, CancellationToken cancellationToken);

    /// <summary>
    /// Gives up a claimed item: it is dead, with its reason.
    /// </summary>
    /// <param name="claim">The claim of the item.</param>
    /// <param name="reason">The reason kept with the item; at most 4000 characters.</param>
    /// <param name="cancellationToken">Ends the wait for the database, as the remarks of <see cref="IWorkStore"/> say.</param>
    /// <returns>
    /// <see langword="true"/> when the item was given up; <see langword="false"/> when the claim's fence is not the
    /// item's current one, and nothing was written.
    /// </returns>
    /// <remarks>
    /// A dead item is kept for the queue's retention; a dead log or run item no longer holds its key. A coalescing item
    /// that an enqueue marked to run again while it was held is revived at once, as an enqueue of a dead item does: it
    /// is waiting at the mark's due time, with its attempts at zero and the reason kept.
    /// </remarks>
    Task<bool> GiveUpAsync(WorkClaim claim, string reason, CancellationToken cancellationToken);

    /// <summary>
    /// Releases a claimed item when its worker stops: it is waiting again at once, and the attempt is given back.
    /// </summary>
    /// <param name="claim">The claim of the item.</param>
    /// <param name="cancellationToken">Ends the wait for the database, as the remarks of <see cref="IWorkStore"/> say.</param>
    /// <returns>
    /// <see langword="true"/> when the item was released; <see langword="false"/> when the claim's fence is not the
    /// item's current one, and nothing was written.
    /// </returns>
    /// <remarks>
    /// The item is due at the database's now, with the attempts it had before the claim, because the stop, not the
    /// item, ended the attempt. A mark to run again stays, as with a retry.
    /// </remarks>
    Task<bool> ReleaseAsync(WorkClaim claim, CancellationToken cancellationToken);

    /// <summary>
    /// Puts a dead item back: it is waiting at the database's now, with its attempts at zero.
    /// </summary>
    /// <param name="queue">The queue of the item.</param>
    /// <param name="itemId">The id of the dead item.</param>
    /// <param name="cancellationToken">Ends the wait for the database, as the remarks of <see cref="IWorkStore"/> say.</param>
    /// <returns>
    /// <see cref="WorkRequeueStatusEnum.Requeued"/> when the item was dead and is waiting again;
    /// <see cref="WorkRequeueStatusEnum.NotDead"/> when the queue has no dead item with that id;
    /// <see cref="WorkRequeueStatusEnum.KeyActive"/> when the dead item is a log or run item whose key an active item
    /// has, also one enqueued at the same moment. Nothing is written but for <c>Requeued</c>.
    /// </returns>
    /// <remarks>
    /// The item keeps its last reason until its next failure. A refusal is reported by its status, never as the
    /// database's unique violation.
    /// </remarks>
    Task<WorkRequeueStatusEnum> RequeueAsync(WorkQueue queue, long itemId, CancellationToken cancellationToken);

    /// <summary>
    /// Says, as durations from the database's now, when the queue has an item to claim next and an item to delete
    /// next.
    /// </summary>
    /// <param name="queue">The queue.</param>
    /// <param name="cancellationToken">Ends the wait for the database, as the remarks of <see cref="IWorkStore"/> say.</param>
    /// <returns>The durations, as <see cref="WorkNextDue"/> describes them.</returns>
    Task<WorkNextDue> GetNextDueAsync(WorkQueue queue, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes at most <paramref name="count"/> of the queue's expired items, oldest first.
    /// </summary>
    /// <param name="queue">The queue whose expired items are deleted.</param>
    /// <param name="count">The most items to delete; at least 1.</param>
    /// <param name="cancellationToken">Ends the wait for the database, as the remarks of <see cref="IWorkStore"/> say.</param>
    /// <returns>The number of items deleted.</returns>
    /// <remarks>
    /// An item is expired when it is finished or dead and ended longer ago than the queue's retention, by the
    /// database's clock. No item that waits or is held is ever deleted.
    /// </remarks>
    Task<int> DeleteExpiredAsync(WorkQueue queue, int count, CancellationToken cancellationToken);
}
