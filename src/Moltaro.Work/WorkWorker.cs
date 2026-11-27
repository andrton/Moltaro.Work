namespace Moltaro.Work;

/// <summary>
/// Runs the handler of one queue: claims due items, runs the handler for each on its lanes, keeps their leases and
/// writes their outcomes, and waits for new work without polling.
/// </summary>
/// <remarks>
/// <para>
/// A worker is created over the application's <see cref="WorkClient"/>, whose store it uses and whose
/// <see cref="Moltaro.Signals.ISignals"/> wakes it. Several workers of one queue, in one process or in many, share its
/// items: a claim is exclusive, and every write of a holder is fenced, so that a holder whose lease was reclaimed
/// cannot complete the item.
/// </para>
/// <para>
/// Every member may be called from any thread at any time, except that <see cref="RunAsync"/> runs once.
/// </para>
/// </remarks>
public sealed class WorkWorker
{
    private readonly WorkClient _client;

    private readonly IWorkHandler _handler;

    private readonly int _lanes;

    private readonly TimeSpan _leaseDuration;

    private readonly TimeSpan _heartbeatInterval;

    private readonly TimeSpan _idleBound;

    private readonly TimeSpan _failureDelay;

    private readonly int _retentionBatchSize;

    private readonly TimeProvider _timeProvider;

    // 1 once RunAsync was called; changed only by Interlocked, so that two calls at once cannot both run.
    private int _runCalled;

    /// <summary>
    /// Creates a worker of a queue, with its options validated and copied. It starts nothing: <see cref="RunAsync"/>
    /// runs it.
    /// </summary>
    /// <param name="client">The client whose store and signals the worker uses.</param>
    /// <param name="queue">The queue whose items the worker handles.</param>
    /// <param name="handler">The handler run for each item.</param>
    /// <param name="options">
    /// The options of the worker; <see langword="null"/> means the defaults of <see cref="WorkWorkerOptions"/>. They
    /// are copied: a later change of the object has no effect on the worker.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="client"/>, <paramref name="queue"/> or <paramref name="handler"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// An option is outside its range, with the name of the property as the
    /// <see cref="ArgumentException.ParamName"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <see cref="WorkWorkerOptions.HeartbeatInterval"/> is above half of <see cref="WorkWorkerOptions.LeaseDuration"/>,
    /// with the <see cref="ArgumentException.ParamName"/> <c>HeartbeatInterval</c>; or
    /// <see cref="WorkWorkerOptions.TimeProvider"/> is <see langword="null"/>, with the
    /// <see cref="ArgumentException.ParamName"/> <c>TimeProvider</c>.
    /// </exception>
    public WorkWorker(WorkClient client, WorkQueue queue, IWorkHandler handler, WorkWorkerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(handler);

        options ??= new WorkWorkerOptions();

        int lanes = options.Lanes;
        TimeSpan leaseDuration = options.LeaseDuration;
        TimeSpan heartbeatInterval = options.HeartbeatInterval;
        TimeSpan idleBound = options.IdleBound;
        TimeSpan failureDelay = options.FailureDelay;
        int retentionBatchSize = options.RetentionBatchSize;
        TimeProvider? timeProvider = options.TimeProvider;

        WorkChecks.CheckRange(lanes, 1, 1000, nameof(WorkWorkerOptions.Lanes));
        WorkChecks.CheckTimeSpan(leaseDuration, WorkChecks.LongestTimer, nameof(WorkWorkerOptions.LeaseDuration));
        WorkChecks.CheckTimeSpan(heartbeatInterval, WorkChecks.LongestTimer, nameof(WorkWorkerOptions.HeartbeatInterval));
        WorkChecks.CheckTimeSpan(idleBound, WorkChecks.LongestTimer, nameof(WorkWorkerOptions.IdleBound));
        WorkChecks.CheckTimeSpan(failureDelay, WorkChecks.LongestTimer, nameof(WorkWorkerOptions.FailureDelay));
        WorkChecks.CheckRange(retentionBatchSize, 1, 100000, nameof(WorkWorkerOptions.RetentionBatchSize));

        // Both are at most the longest timer, so twice the interval cannot overflow; ticks compare without rounding.
        if (heartbeatInterval.Ticks * 2 > leaseDuration.Ticks)
        {
            throw new ArgumentException(
                "The heartbeat interval is at most half of the lease duration.",
                nameof(WorkWorkerOptions.HeartbeatInterval));
        }

        if (timeProvider is null)
        {
            throw new ArgumentException("The time provider is required.", nameof(WorkWorkerOptions.TimeProvider));
        }

        _client = client;
        _handler = handler;
        _lanes = lanes;
        _leaseDuration = leaseDuration;
        _heartbeatInterval = heartbeatInterval;
        _idleBound = idleBound;
        _failureDelay = failureDelay;
        _retentionBatchSize = retentionBatchSize;
        _timeProvider = timeProvider;
        Queue = queue;
    }

    /// <summary>
    /// The queue whose items the worker handles.
    /// </summary>
    public WorkQueue Queue { get; }

    /// <summary>
    /// Raised once for every statement of the worker that failed, before the worker waits
    /// <see cref="WorkWorkerOptions.FailureDelay"/> and tries again.
    /// </summary>
    /// <remarks>
    /// It is raised on the worker's loop, never while the worker holds a lock, so a subscriber delays the worker while
    /// it runs. An exception thrown by a subscriber ends <see cref="RunAsync"/> with that exception, after the worker
    /// stopped as its stop does.
    /// </remarks>
#pragma warning disable CS0067 // Raised by the worker's loop, which comes with the implementation of RunAsync.
    public event EventHandler<WorkWorkerFailedEventArgs>? Failed;
#pragma warning restore CS0067

    /// <summary>
    /// Runs the worker until <paramref name="stoppingToken"/> is cancelled. Not implemented yet.
    /// </summary>
    /// <param name="stoppingToken">
    /// The stop of the worker. A token that is already cancelled gives a cancelled task, and nothing is done; once the
    /// worker runs, its cancellation stops the worker and the task completes.
    /// </param>
    /// <returns>A task that completes when the worker has stopped and every handler has ended.</returns>
    /// <remarks>
    /// <para>
    /// The worker first calls <see cref="IWorkStore.CheckDatabaseAsync"/>; an
    /// <see cref="InvalidOperationException"/> of it, a schema version or a database setting that does not fit, ends
    /// the run. It then joins its queue's signal, as a member of the group <c>moltaro.work</c> on the queue's key, and
    /// does one pass after every wait: it claims as many due items as it has free lanes, runs the handler for each on
    /// a lane of its own, renews their leases every <see cref="WorkWorkerOptions.HeartbeatInterval"/>, and writes each
    /// outcome with the claim's fence: a completion; a retry after the queue's backoff, for a failure or a
    /// <see cref="WorkHandlerResultEnum.Retry"/>; or giving up, at the queue's attempt limit. It deletes the queue's
    /// expired items in batches of <see cref="WorkWorkerOptions.RetentionBatchSize"/>.
    /// </para>
    /// <para>
    /// Between passes it waits for its queue's signal, or until the earliest time the database says something is
    /// claimable or expired, bounded by <see cref="WorkWorkerOptions.IdleBound"/>; it sends no statement while it
    /// waits. It does not wait for lease ends, which heartbeats keep moving: an item whose holder died is claimed by
    /// the next claim of any worker after its lease ended, at the latest after an idle bound. A due item that a claim
    /// did not take because a transaction keeps it locked is waited for in the same way. Because a set of a signal
    /// wakes one member of the group, a worker that knows of claimable work it does not take sets the queue's key
    /// itself: after a claim that filled its free lanes, when a due time it knows of is reached while no lane is free,
    /// and after a write of its own that makes an item waiting at once.
    /// </para>
    /// <para>
    /// When <paramref name="stoppingToken"/> is cancelled, the worker cancels the token of every running handler and
    /// waits for each. An attempt that ends after the stop began with anything but
    /// <see cref="WorkHandlerResultEnum.Completed"/> is released: the item is waiting at once with the attempts it had
    /// before the claim, so a failure during a stop keeps neither its reason nor its backoff. Each outcome is written
    /// once; a write that fails leaves its item to its lease. The task completes once every handler has ended.
    /// </para>
    /// <para>
    /// A database error never ends the run: the statement that failed raises <see cref="Failed"/>, and the worker
    /// tries again after <see cref="WorkWorkerOptions.FailureDelay"/>. A wait for the signal that fails other than by
    /// the stop's cancellation ends the run with that exception, after the worker stopped, because the worker could
    /// then wait for nothing.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown by the call when <see cref="RunAsync"/> was called before. Reported by the task when
    /// <see cref="IWorkStore.CheckDatabaseAsync"/> refused the database.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown by the call after its checks: running a worker is not implemented yet.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Reported by the task when <paramref name="stoppingToken"/> was already cancelled when the method was called.
    /// </exception>
    public Task RunAsync(CancellationToken stoppingToken)
    {
        if (Interlocked.Exchange(ref _runCalled, 1) == 1)
        {
            throw new InvalidOperationException("RunAsync was called before: a worker runs once.");
        }

        if (stoppingToken.IsCancellationRequested)
        {
            return Task.FromCanceled(stoppingToken);
        }

        throw new NotSupportedException("Running a worker is not implemented yet.");
    }
}
