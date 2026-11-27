namespace Moltaro.Work;

/// <summary>
/// The options of a <see cref="WorkWorker"/>, with their defaults.
/// </summary>
/// <remarks>
/// The constructor of <see cref="WorkWorker"/> validates the options and copies them: a later change of this object
/// has no effect on the worker. A number or a <see cref="TimeSpan"/> outside its range gives
/// <see cref="ArgumentOutOfRangeException"/>; any other wrong value gives <see cref="ArgumentException"/>. The
/// <see cref="ArgumentException.ParamName"/> is the name of the property. A <see cref="TimeSpan"/> option is greater
/// than zero and at most <c>TimeSpan.FromMilliseconds(uint.MaxValue - 1)</c>, the longest timer of
/// <see cref="System.TimeProvider"/>. The timers of the worker run on the clock of its <see cref="WorkClient"/>.
/// </remarks>
public sealed class WorkWorkerOptions
{
    /// <summary>
    /// The number of handlers that run at once, and the most items one claim asks for: a claim asks for as many as
    /// there are free lanes. 1 to 1000; 1 by default.
    /// </summary>
    public int Lanes { get; set; } = 1;

    /// <summary>
    /// The lease that each claim and each heartbeat sets, counted by the database's clock. Greater than zero and at
    /// most the longest timer of <see cref="System.TimeProvider"/>; 30 seconds by default.
    /// </summary>
    /// <remarks>
    /// An item whose lease ends without a completion may be claimed again by any worker, and that counts as an
    /// attempt.
    /// </remarks>
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How often the worker renews the leases of the items it holds. Greater than zero and at most half of
    /// <see cref="LeaseDuration"/>; 10 seconds by default.
    /// </summary>
    /// <remarks>
    /// Above half of <see cref="LeaseDuration"/> it gives <see cref="ArgumentException"/>, so that one late heartbeat
    /// does not lose a lease.
    /// </remarks>
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The longest the worker waits while nothing it knows is due. Greater than zero and at most the longest timer of
    /// <see cref="System.TimeProvider"/>; 1 minute by default.
    /// </summary>
    /// <remarks>
    /// The worker waits for its queue's signal or until the earliest due time it knows, whichever comes first. This
    /// bound alone limits how long an item waits when the process that committed it died before setting the signal.
    /// </remarks>
    public TimeSpan IdleBound { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// How long the worker waits after one of its own statements failed, before it tries again. Greater than zero and
    /// at most the longest timer of <see cref="System.TimeProvider"/>; 5 seconds by default.
    /// </summary>
    /// <remarks>
    /// A failure of the database, such as an unreachable server, never stops the worker: it is reported through
    /// <see cref="WorkWorker.Failed"/>, and the worker goes on after this delay.
    /// </remarks>
    public TimeSpan FailureDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The most items one delete of expired items removes. 1 to 100000; 1000 by default.
    /// </summary>
    public int RetentionBatchSize { get; set; } = 1000;
}
