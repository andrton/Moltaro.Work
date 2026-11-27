namespace Moltaro.Work.TestSupport.Conformance;

// Claims, leases and fences (rules 4, 5, 6 and 9 of concept section 3): a claim is exclusive, a heartbeat keeps a
// lease, an ended lease makes the item claimable again as another attempt, and only the current holder writes.
public abstract class ClaimsConformance
{
    private readonly ConformanceDatabase _database;

    protected ClaimsConformance(ConformanceDatabase database)
    {
        _database = database;
    }

    // Each claimer has a store of its own, as the workers of several processes do, and claims until it finds nothing.
    [Fact(Skip = SkipReason.NotImplemented)]
    public async Task Many_claimers_at_once_never_return_one_item_twice()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        WorkQueue queue = ConformanceDatabase.CreateQueue(WorkShapeEnum.Coalescing);
        IWorkStore store = _database.CreateStore();

        for (int invoice = 1; invoice <= 40; invoice++)
        {
            await _database.EnqueueCommittedAsync(store, queue, $"invoice-{invoice}", null, cancellationToken);
        }

        List<WorkClaim>[] claimers = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(_ => ClaimUntilNothingAsync(_database.CreateStore(), queue, cancellationToken)));

        long[] claimedIds = claimers.SelectMany(claims => claims).Select(claim => claim.Item.Id).ToArray();
        Assert.NotEmpty(claimedIds);
        Assert.Equal(claimedIds.Length, claimedIds.Distinct().Count());
    }

    [Fact(Skip = SkipReason.NotImplemented)]
    public async Task A_heartbeat_moves_the_lease()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        WorkQueue queue = ConformanceDatabase.CreateQueue(WorkShapeEnum.Coalescing);
        IWorkStore store = _database.CreateStore();
        await _database.EnqueueCommittedAsync(store, queue, "invoice-7", null, cancellationToken);
        WorkClaim claim = Assert.Single(await store.ClaimAsync(queue, 1, ConformanceDatabase.Lease, cancellationToken));

        // The lease of 30 seconds has 10 left when the heartbeat renews it for 30 more.
        await _database.MoveClockAsync(queue, TimeSpan.FromSeconds(20), cancellationToken);
        Assert.Empty(await store.HeartbeatAsync([claim], ConformanceDatabase.Lease, cancellationToken));
        await _database.MoveClockAsync(queue, TimeSpan.FromSeconds(20), cancellationToken);

        Assert.Empty(await store.ClaimAsync(queue, 1, ConformanceDatabase.Lease, cancellationToken));
    }

    [Fact(Skip = SkipReason.NotImplemented)]
    public async Task An_item_whose_lease_ended_is_claimed_again_as_its_next_attempt_with_a_new_fence()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        WorkQueue queue = ConformanceDatabase.CreateQueue(WorkShapeEnum.Coalescing);
        IWorkStore store = _database.CreateStore();
        await _database.EnqueueCommittedAsync(store, queue, "invoice-7", null, cancellationToken);
        WorkClaim first = Assert.Single(await store.ClaimAsync(queue, 1, ConformanceDatabase.Lease, cancellationToken));

        Assert.Empty(await store.ClaimAsync(queue, 1, ConformanceDatabase.Lease, cancellationToken));

        await _database.MoveClockAsync(queue, ConformanceDatabase.Lease + TimeSpan.FromSeconds(1), cancellationToken);
        WorkClaim second = Assert.Single(await store.ClaimAsync(queue, 1, ConformanceDatabase.Lease, cancellationToken));

        Assert.Equal(first.Item.Id, second.Item.Id);
        Assert.Equal(1, first.Item.Attempt);
        Assert.Equal(2, second.Item.Attempt);
        Assert.True(second.Fence > first.Fence);
    }

    [Fact(Skip = SkipReason.NotImplemented)]
    public async Task The_heartbeat_and_the_completion_of_a_holder_whose_item_was_claimed_again_are_refused()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        WorkQueue queue = ConformanceDatabase.CreateQueue(WorkShapeEnum.Coalescing);
        IWorkStore store = _database.CreateStore();
        (WorkClaim reclaimed, WorkClaim current) = await ClaimTwiceAsync(store, queue, cancellationToken);

        Assert.Same(reclaimed, Assert.Single(await store.HeartbeatAsync([reclaimed], ConformanceDatabase.Lease, cancellationToken)));
        Assert.False(await store.CompleteAsync(reclaimed, cancellationToken));

        // The refusals wrote nothing: the item is still held with the current fence.
        Assert.Empty(await store.HeartbeatAsync([current], ConformanceDatabase.Lease, cancellationToken));
    }

    [Fact(Skip = SkipReason.NotImplemented)]
    public async Task Of_two_holders_of_one_item_only_the_current_one_completes_it()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        WorkQueue queue = ConformanceDatabase.CreateQueue(WorkShapeEnum.Coalescing);
        IWorkStore store = _database.CreateStore();
        (WorkClaim reclaimed, WorkClaim current) = await ClaimTwiceAsync(store, queue, cancellationToken);

        Assert.False(await store.CompleteAsync(reclaimed, cancellationToken));
        Assert.True(await store.CompleteAsync(current, cancellationToken));

        // The completed coalescing item is deleted, not merely held.
        await _database.MoveClockAsync(queue, ConformanceDatabase.Lease + TimeSpan.FromSeconds(1), cancellationToken);
        Assert.Empty(await store.ClaimAsync(queue, 1, ConformanceDatabase.Lease, cancellationToken));
    }

    private static async Task<List<WorkClaim>> ClaimUntilNothingAsync(
        IWorkStore store,
        WorkQueue queue,
        CancellationToken cancellationToken)
    {
        var claims = new List<WorkClaim>();

        while (true)
        {
            IReadOnlyList<WorkClaim> batch = await store.ClaimAsync(queue, 3, ConformanceDatabase.Lease, cancellationToken);

            if (batch.Count == 0)
            {
                return claims;
            }

            claims.AddRange(batch);
        }
    }

    // One item claimed, its lease ended, and claimed again: the first claim's fence is no longer the item's.
    private async Task<(WorkClaim Reclaimed, WorkClaim Current)> ClaimTwiceAsync(
        IWorkStore store,
        WorkQueue queue,
        CancellationToken cancellationToken)
    {
        await _database.EnqueueCommittedAsync(store, queue, "invoice-7", null, cancellationToken);
        WorkClaim reclaimed = Assert.Single(await store.ClaimAsync(queue, 1, ConformanceDatabase.Lease, cancellationToken));

        await _database.MoveClockAsync(queue, ConformanceDatabase.Lease + TimeSpan.FromSeconds(1), cancellationToken);
        WorkClaim current = Assert.Single(await store.ClaimAsync(queue, 1, ConformanceDatabase.Lease, cancellationToken));

        return (reclaimed, current);
    }
}
