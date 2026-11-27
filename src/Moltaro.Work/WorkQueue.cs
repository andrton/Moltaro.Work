namespace Moltaro.Work;

/// <summary>
/// The declaration of a queue: its name, its shape and its policies.
/// </summary>
/// <remarks>
/// <para>
/// A queue is a declaration, with no database behind it: creating one reads and writes nothing. Its items live in
/// the tables of the provider, which the application creates with the provider's schema script.
/// </para>
/// <para>
/// A name means one queue. Every process that uses it declares it with the same shape and options; the library
/// cannot check that across processes.
/// </para>
/// <para>
/// A name is 1 to 100 characters, each one of <c>A–Z a–z 0–9 _ -</c>, and compares exactly, case included.
/// </para>
/// <para>
/// The workers of a queue are members of the Moltaro.Signals group <c>moltaro.work</c> on the key
/// <c>moltaro.work.</c> followed by the queue's name, and a commit that enqueued into the queue sets that key. A name
/// has no <c>.</c>, so a set never reaches the key of another queue as an ancestor.
/// </para>
/// <para>
/// A queue does not change after its constructor, and every member may be called from any thread at any time.
/// </para>
/// </remarks>
public sealed class WorkQueue
{
    /// <summary>
    /// Declares a queue, with its options validated and copied.
    /// </summary>
    /// <param name="name">The name of the queue, by the rule of the remarks of <see cref="WorkQueue"/>.</param>
    /// <param name="shape">The shape of the queue's items.</param>
    /// <param name="options">
    /// The policies of the queue; <see langword="null"/> means the defaults of <see cref="WorkQueueOptions"/>. They are
    /// copied: a later change of the object has no effect on the queue.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> breaks the rule of a queue name; or <see cref="WorkQueueOptions.MaxRetryDelay"/> is
    /// below <see cref="WorkQueueOptions.RetryDelay"/>, with the <see cref="ArgumentException.ParamName"/>
    /// <c>MaxRetryDelay</c>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="shape"/> is not a value of <see cref="WorkShapeEnum"/>; or an option is outside its range, with
    /// the name of the property as the <see cref="ArgumentException.ParamName"/>.
    /// </exception>
    public WorkQueue(string name, WorkShapeEnum shape, WorkQueueOptions? options = null)
    {
        WorkChecks.CheckQueueName(name, nameof(name));

        if (!Enum.IsDefined(shape))
        {
            throw new ArgumentOutOfRangeException(nameof(shape), shape, "The shape is not a value of WorkShapeEnum.");
        }

        options ??= new WorkQueueOptions();

        int attemptLimit = options.AttemptLimit;
        TimeSpan retryDelay = options.RetryDelay;
        TimeSpan maxRetryDelay = options.MaxRetryDelay;
        TimeSpan retention = options.Retention;

        WorkChecks.CheckRange(attemptLimit, 1, 1000, nameof(WorkQueueOptions.AttemptLimit));
        WorkChecks.CheckTimeSpan(retryDelay, WorkChecks.LongestQueueTime, nameof(WorkQueueOptions.RetryDelay));
        WorkChecks.CheckTimeSpan(maxRetryDelay, WorkChecks.LongestQueueTime, nameof(WorkQueueOptions.MaxRetryDelay));
        WorkChecks.CheckTimeSpan(retention, WorkChecks.LongestQueueTime, nameof(WorkQueueOptions.Retention));

        if (maxRetryDelay < retryDelay)
        {
            throw new ArgumentException(
                "The longest retry delay is at least the first one.",
                nameof(WorkQueueOptions.MaxRetryDelay));
        }

        Name = name;
        Shape = shape;
        AttemptLimit = attemptLimit;
        RetryDelay = retryDelay;
        MaxRetryDelay = maxRetryDelay;
        Retention = retention;
        Scoped = options.Scoped;
    }

    /// <summary>
    /// The name of the queue, which identifies its items in the database and its signal.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The shape of the queue's items.
    /// </summary>
    public WorkShapeEnum Shape { get; }

    /// <summary>
    /// The most attempts of an item before it is dead, as <see cref="WorkQueueOptions.AttemptLimit"/> says.
    /// </summary>
    public int AttemptLimit { get; }

    /// <summary>
    /// The delay after the first failed attempt, doubling after each further one up to <see cref="MaxRetryDelay"/>.
    /// </summary>
    public TimeSpan RetryDelay { get; }

    /// <summary>
    /// The longest delay before a retry.
    /// </summary>
    public TimeSpan MaxRetryDelay { get; }

    /// <summary>
    /// How long dead items and finished runs are kept, from the time they ended, before they are deleted.
    /// </summary>
    public TimeSpan Retention { get; }

    /// <summary>
    /// Whether the items of the queue may carry a scope: an item is not claimed while another item of its scope is
    /// held.
    /// </summary>
    public bool Scoped { get; }
}
