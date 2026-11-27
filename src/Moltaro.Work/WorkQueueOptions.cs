namespace Moltaro.Work;

/// <summary>
/// The policies of a queue, with their defaults.
/// </summary>
/// <remarks>
/// The constructor of <see cref="WorkQueue"/> validates the options and copies them: a later change of this object
/// has no effect on the queue. A number or a <see cref="TimeSpan"/> outside its range gives
/// <see cref="ArgumentOutOfRangeException"/>, and <see cref="MaxRetryDelay"/> below <see cref="RetryDelay"/> gives
/// <see cref="ArgumentException"/>; the <see cref="ArgumentException.ParamName"/> is the name of the property. A
/// <see cref="TimeSpan"/> option is greater than zero and at most 3650 days.
/// </remarks>
public sealed class WorkQueueOptions
{
    /// <summary>
    /// The most attempts of an item before it is dead. 1 to 1000; 10 by default.
    /// </summary>
    /// <remarks>
    /// An attempt is one claim, so a lease that ends without a completion counts as one.
    /// </remarks>
    public int AttemptLimit { get; set; } = 10;

    /// <summary>
    /// The delay after the first failed attempt; it doubles after each further one, up to <see cref="MaxRetryDelay"/>.
    /// Greater than zero and at most 3650 days; 10 seconds by default.
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The longest delay before a retry. At least <see cref="RetryDelay"/> and at most 3650 days; 1 hour by default.
    /// </summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// How long dead items and finished runs are kept, from the time they ended, before they are deleted. Greater than
    /// zero and at most 3650 days; 7 days by default.
    /// </summary>
    public TimeSpan Retention { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Whether the items of the queue may carry a scope, a second key of which at most one item is held at a time.
    /// <see langword="false"/> by default.
    /// </summary>
    public bool Scoped { get; set; }
}
