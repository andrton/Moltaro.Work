namespace Moltaro.Work;

/// <summary>
/// What an enqueue did. The value 0 is never returned, so a <see langword="default"/> result claims nothing.
/// </summary>
public enum WorkEnqueueStatusEnum
{
    /// <summary>
    /// An item became waiting that was neither waiting nor held before: a new item, or a dead coalescing item revived
    /// by the enqueue.
    /// </summary>
    Added = 1,

    /// <summary>
    /// An item already waiting or held absorbed the enqueue: a coalescing item of the same key, or a log or run item
    /// that is active with the same key. Nothing new was added; what the enqueue changed in that item is said by
    /// <see cref="WorkShapeEnum"/>.
    /// </summary>
    Coalesced = 2,
}
