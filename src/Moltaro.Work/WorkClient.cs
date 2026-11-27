using System.Data.Common;
using Moltaro.Signals;

namespace Moltaro.Work;

/// <summary>
/// The library as an application uses it: joins the application's transactions to enqueue items in them, and puts
/// dead items back.
/// </summary>
/// <remarks>
/// <para>
/// One client serves the whole application: it holds the provider's store and the <see cref="ISignals"/> through
/// which commits wake the workers. It owns neither of them; whoever created them disposes them. The workers of the
/// application are created over the same client.
/// </para>
/// <para>
/// Every member may be called from any thread at any time. An argument error is thrown by the call, before anything
/// happens; a failure of the operation is reported by the returned task.
/// </para>
/// </remarks>
public sealed class WorkClient
{
    /// <summary>
    /// Creates a client over a store, a signals client or host, and the clock of its workers.
    /// </summary>
    /// <param name="store">The store of the provider of the application's database.</param>
    /// <param name="signals">
    /// The Moltaro.Signals client or in-process host through which commits set the signals of their queues and
    /// workers wait for them.
    /// </param>
    /// <param name="timeProvider">
    /// The clock of the timers of the client's workers: their heartbeats, their failure delays and the deadlines of
    /// their waits. <see langword="null"/> means <see cref="TimeProvider.System"/>, which is also the default clock of
    /// <see cref="SignalHost"/> and <see cref="SignalClient"/>.
    /// </param>
    /// <remarks>
    /// The clock has to be the one of <paramref name="signals"/>. A worker passes the deadline of each wait to its
    /// subscription as an instant: it adds the durations that the database computed, from the database's now to the
    /// next claimable time and to the next expiry, to the now of this clock, and the subscription compares that
    /// instant with the clock of the object that created it. A duration compares no two clocks, so due times and lease
    /// ends stay the database's; the error is the drift of one clock during one wait. In production both are
    /// <see cref="TimeProvider.System"/>; tests give both the same fake clock. The client cannot check it, because an
    /// <see cref="ISignals"/> does not expose its clock. Every worker of the client uses this one clock.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="store"/> or <paramref name="signals"/> is <see langword="null"/>.</exception>
    public WorkClient(IWorkStore store, ISignals signals, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(signals);

        Store = store;
        Signals = signals;
        TimeProvider = timeProvider ?? TimeProvider.System;
    }

    // The store every enqueue, requeue and worker of this client goes through.
    internal IWorkStore Store { get; }

    // The signals that commits and requeues set and that workers wait for.
    internal ISignals Signals { get; }

    // The clock of the timers of every worker of this client; the clock of Signals (W6).
    internal TimeProvider TimeProvider { get; }

    /// <summary>
    /// Joins a transaction of the application, so that items are enqueued in it and it is committed through the
    /// library.
    /// </summary>
    /// <param name="transaction">
    /// The application's open transaction, on a connection of the store's database. It stays the application's: the
    /// application rolls it back on failure and disposes it.
    /// </param>
    /// <returns>A new <see cref="WorkTransaction"/> over <paramref name="transaction"/>, returned at once.</returns>
    /// <remarks>
    /// It neither reads nor changes the transaction. Each call returns a new object; an application joins a
    /// transaction once and enqueues through that one object.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="transaction"/> is <see langword="null"/>.</exception>
    public WorkTransaction Join(DbTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        return new WorkTransaction(Store, transaction);
    }

    /// <summary>
    /// Puts a dead item back: it is waiting at once, with its attempts counted from zero. Not implemented yet.
    /// </summary>
    /// <param name="queue">The queue of the item.</param>
    /// <param name="itemId">The id of the dead item.</param>
    /// <param name="cancellationToken">
    /// Ends the wait for the database. A token that is already cancelled gives a cancelled task, and nothing is
    /// written. Once the requeue has committed, the task completes even if the token is cancelled.
    /// </param>
    /// <returns>
    /// <see cref="WorkRequeueStatusEnum.Requeued"/> when the item was dead and is waiting again;
    /// <see cref="WorkRequeueStatusEnum.NotDead"/> when the queue has no dead item with that id, and nothing was
    /// written; <see cref="WorkRequeueStatusEnum.KeyActive"/> when the dead item is a log or run item whose key now
    /// belongs to an active item, and nothing was written.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The item keeps its last reason until its next failure. The requeue runs on the store's own connection, in a
    /// transaction of its own, and only after that transaction committed it sets the queue's signal, so that a worker
    /// takes the item. A requeue is refused with a status of its own, never with the database's unique violation, also
    /// when an item with the same key is enqueued at the same moment.
    /// </para>
    /// <para>
    /// A set of the signal that is lost or fails is not reported: the workers' idle bound covers it.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown by the call: <paramref name="queue"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown by the call: <paramref name="itemId"/> is below 1.</exception>
    /// <exception cref="NotSupportedException">
    /// Thrown by the call after its argument checks: requeueing is not implemented yet.
    /// </exception>
    /// <exception cref="DbException">Reported by the task: the database failed; the driver's exception.</exception>
    /// <exception cref="OperationCanceledException">
    /// Reported by the task: <paramref name="cancellationToken"/> was cancelled before the requeue committed.
    /// </exception>
    public Task<WorkRequeueStatusEnum> RequeueAsync(WorkQueue queue, long itemId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentOutOfRangeException.ThrowIfLessThan(itemId, 1);

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<WorkRequeueStatusEnum>(cancellationToken);
        }

        throw new NotSupportedException("Requeueing a dead item is not implemented yet.");
    }
}
