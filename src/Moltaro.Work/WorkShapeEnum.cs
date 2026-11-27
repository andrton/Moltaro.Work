namespace Moltaro.Work;

/// <summary>
/// The shape of a queue: what one item stands for, what an enqueue does while an item of the same key waits or is
/// held, and what becomes of an item after its handler succeeded.
/// </summary>
/// <remarks>
/// The three shapes share one mechanism: claiming, leases, fences, retries, dead items and retention work the same
/// way in each. The value 0 is no shape, and <see cref="WorkQueue"/> refuses it.
/// </remarks>
public enum WorkShapeEnum
{
    /// <summary>
    /// One item per key, saying "this object needs attention". The key names the object, the item carries no payload,
    /// and the handler reads the object's current state.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A queue keeps one item per key in every state, so every enqueue names a key.
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// <b>While the item waits</b>, an enqueue adds nothing and reports <see cref="WorkEnqueueStatusEnum.Coalesced"/>.
    /// If the item's attempts are at zero, its due time becomes the earlier of the two; an item that waits for a retry
    /// keeps its due time, so that its backoff holds.
    /// </description></item>
    /// <item><description>
    /// <b>While the item is held</b>, an enqueue reports <see cref="WorkEnqueueStatusEnum.Coalesced"/> and marks the
    /// item to run once more, at the earliest due time any such enqueue asked for. When the attempt ends, a completion
    /// makes the item waiting again at that time, with its attempts counted from zero; giving up does the same, as an
    /// enqueue of a dead item would; a retry or a release keeps the mark for the next attempt.
    /// </description></item>
    /// <item><description>
    /// <b>While the item is dead</b>, an enqueue revives it and reports <see cref="WorkEnqueueStatusEnum.Added"/>: the
    /// item is waiting with the due time of the enqueue and its attempts counted from zero; its last reason stays
    /// until the next failure.
    /// </description></item>
    /// <item><description>
    /// <b>After success</b> the item is deleted.
    /// </description></item>
    /// </list>
    /// <para>
    /// The scope of the item is the one of the enqueue that added it; a later enqueue does not change it.
    /// </para>
    /// </remarks>
    Coalescing = 1,

    /// <summary>
    /// One item per enqueue: an event that has to be handled, with a payload that the application serializes.
    /// </summary>
    /// <remarks>
    /// Every enqueue adds an item, whether another one waits or is held. A key is optional and makes the enqueue
    /// idempotent: it is unique among the queue's active items, those waiting or held, and an enqueue whose key an
    /// active item has adds nothing and reports <see cref="WorkEnqueueStatusEnum.Coalesced"/> with that item's id. A
    /// dead item no longer holds its key. After success the item is deleted; an application that wants the history
    /// declares a <see cref="Run"/> queue.
    /// </remarks>
    Log = 2,

    /// <summary>
    /// One item per enqueue, kept with its outcome so that an application can show its history.
    /// </summary>
    /// <remarks>
    /// Every enqueue adds an item, with a payload that the application serializes. A key is optional and makes the
    /// enqueue idempotent: it is unique among the queue's active items, those waiting or held, and an enqueue whose key
    /// an active item has adds nothing and reports <see cref="WorkEnqueueStatusEnum.Coalesced"/> with that item's id.
    /// After success the item is kept as finished, with its attempts, its times and its last reason, for the queue's
    /// <see cref="WorkQueue.Retention"/>. A finished or dead item no longer holds its key.
    /// </remarks>
    Run = 3,
}
