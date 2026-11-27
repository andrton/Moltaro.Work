namespace Moltaro.Work;

/// <summary>
/// An item as one claim took it: what a handler gets for its attempt.
/// </summary>
/// <remarks>
/// A provider creates it from a row of its table; an application reads it. It does not change after its constructor,
/// and every member may be called from any thread at any time.
/// </remarks>
public sealed class WorkItem
{
    /// <summary>
    /// Creates an item as a claim took it.
    /// </summary>
    /// <param name="queue">The queue of the item.</param>
    /// <param name="id">The id the database assigned to the item; at least 1.</param>
    /// <param name="key">The key of the item, or <see langword="null"/>.</param>
    /// <param name="scope">The scope of the item, or <see langword="null"/>.</param>
    /// <param name="payload">The payload of the item, or <see langword="null"/>.</param>
    /// <param name="attempt">The number of this attempt; at least 1.</param>
    /// <remarks>
    /// Nothing else is checked, because a provider creates it from a row that the enqueue checked.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="queue"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="id"/> or <paramref name="attempt"/> is below 1.</exception>
    public WorkItem(WorkQueue queue, long id, string? key, string? scope, string? payload, int attempt)
    {
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentOutOfRangeException.ThrowIfLessThan(id, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(attempt, 1);

        Queue = queue;
        Id = id;
        Key = key;
        Scope = scope;
        Payload = payload;
        Attempt = attempt;
    }

    /// <summary>
    /// The queue of the item.
    /// </summary>
    public WorkQueue Queue { get; }

    /// <summary>
    /// The id of the item, assigned by the database when the item was added; the same in every attempt.
    /// </summary>
    public long Id { get; }

    /// <summary>
    /// The key of the item: always present for a coalescing item, where it names the object to look at;
    /// <see langword="null"/> for a log or run item enqueued without one.
    /// </summary>
    public string? Key { get; }

    /// <summary>
    /// The scope of the item, or <see langword="null"/> when it has none.
    /// </summary>
    public string? Scope { get; }

    /// <summary>
    /// The payload of a log or run item, as the application enqueued it; <see langword="null"/> for a coalescing item
    /// and for an item enqueued without one.
    /// </summary>
    public string? Payload { get; }

    /// <summary>
    /// The number of this attempt, 1 for the first. Every claim counts, also one whose lease was lost, except a claim
    /// that a worker gave back by a release when it stopped. The count starts again from zero when the item is
    /// revived, requeued, or made waiting again by an enqueue while it was held.
    /// </summary>
    public int Attempt { get; }
}
