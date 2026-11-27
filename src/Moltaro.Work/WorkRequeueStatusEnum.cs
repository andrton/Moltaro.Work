namespace Moltaro.Work;

/// <summary>
/// What a requeue did. The value 0 is never returned, so a <see langword="default"/> result claims nothing.
/// </summary>
public enum WorkRequeueStatusEnum
{
    /// <summary>
    /// The item was dead and is waiting again, at once, with its attempts counted from zero.
    /// </summary>
    Requeued = 1,

    /// <summary>
    /// The queue has no dead item with that id: it is waiting, held, finished, deleted, of another queue, or was never
    /// there. Nothing was written.
    /// </summary>
    NotDead = 2,

    /// <summary>
    /// The dead item is a log or run item whose key belongs to an active item, one waiting or held, also one enqueued
    /// at the same moment. Nothing was written.
    /// </summary>
    KeyActive = 3,
}
