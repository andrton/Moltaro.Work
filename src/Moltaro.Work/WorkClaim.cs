namespace Moltaro.Work;

/// <summary>
/// One claim of an item: the item as it was claimed, and the fence that this claim was given.
/// </summary>
/// <remarks>
/// A provider creates it when a claim takes the item. The fence grows with every claim of the item and is never
/// reset, so the item's id and the fence name one claim and never repeat. Every later write of the holder presents the
/// claim, and the provider refuses it when the fence is no longer the item's current one. It does not change after its
/// constructor, and every member may be called from any thread at any time.
/// </remarks>
public sealed class WorkClaim
{
    /// <summary>
    /// Creates a claim of an item.
    /// </summary>
    /// <param name="item">The item as the claim took it.</param>
    /// <param name="fence">The fence of this claim; at least 1.</param>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="fence"/> is below 1.</exception>
    public WorkClaim(WorkItem item, long fence)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentOutOfRangeException.ThrowIfLessThan(fence, 1);

        Item = item;
        Fence = fence;
    }

    /// <summary>
    /// The item as the claim took it.
    /// </summary>
    public WorkItem Item { get; }

    /// <summary>
    /// The fence of this claim, which every write of its holder presents.
    /// </summary>
    public long Fence { get; }
}
