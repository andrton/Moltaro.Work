namespace Moltaro.Work;

/// <summary>
/// The outcome of an enqueue: what it did, and to which item.
/// </summary>
/// <param name="Status">Whether an item was added or an existing one absorbed the enqueue.</param>
/// <param name="ItemId">
/// The id of the added item, or of the item that absorbed the enqueue: a number of at least 1, assigned by the
/// database.
/// </param>
/// <remarks>
/// A <see langword="default"/> value has the status 0, which no enqueue returns.
/// </remarks>
public readonly record struct WorkEnqueueResult(WorkEnqueueStatusEnum Status, long ItemId);
