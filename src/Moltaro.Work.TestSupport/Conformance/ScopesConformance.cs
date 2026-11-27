namespace Moltaro.Work.TestSupport.Conformance;

// Scopes (rule 10 of concept section 3): in a scoped queue no item is claimed while another item of its scope is
// held, also under concurrent claimers.
public abstract class ScopesConformance
{
    private readonly ConformanceDatabase _database;

    protected ScopesConformance(ConformanceDatabase database)
    {
        _database = database;
    }

    [Fact(Skip = SkipReason.NotImplemented)]
    public async Task No_item_is_claimed_while_another_item_of_its_scope_is_held()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        WorkQueue queue = ConformanceDatabase.CreateQueue(WorkShapeEnum.Coalescing, new WorkQueueOptions { Scoped = true });
        IWorkStore store = _database.CreateStore();
        await _database.EnqueueCommittedAsync(store, queue, "invoice-1", "customer-7", cancellationToken);
        await _database.EnqueueCommittedAsync(store, queue, "invoice-2", "customer-7", cancellationToken);

        WorkClaim first = Assert.Single(await store.ClaimAsync(queue, 10, ConformanceDatabase.Lease, cancellationToken));
        Assert.Empty(await store.ClaimAsync(queue, 10, ConformanceDatabase.Lease, cancellationToken));

        Assert.True(await store.CompleteAsync(first, cancellationToken));
        WorkClaim second = Assert.Single(await store.ClaimAsync(queue, 10, ConformanceDatabase.Lease, cancellationToken));

        Assert.NotEqual(first.Item.Id, second.Item.Id);
        Assert.Equal("customer-7", second.Item.Scope);
    }

    // Nothing is completed and no lease ends, so every claim returned is held at the same time.
    [Fact(Skip = SkipReason.NotImplemented)]
    public async Task Concurrent_claimers_never_hold_two_items_of_one_scope_at_once()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        WorkQueue queue = ConformanceDatabase.CreateQueue(WorkShapeEnum.Coalescing, new WorkQueueOptions { Scoped = true });
        IWorkStore store = _database.CreateStore();

        for (int customer = 1; customer <= 4; customer++)
        {
            for (int invoice = 1; invoice <= 5; invoice++)
            {
                await _database.EnqueueCommittedAsync(
                    store,
                    queue,
                    $"invoice-{customer}-{invoice}",
                    $"customer-{customer}",
                    cancellationToken);
            }
        }

        IReadOnlyList<WorkClaim>[] batches = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(
                _ => _database.CreateStore().ClaimAsync(queue, 5, ConformanceDatabase.Lease, cancellationToken)));

        string?[] heldScopes = batches.SelectMany(batch => batch).Select(claim => claim.Item.Scope).ToArray();
        Assert.NotEmpty(heldScopes);
        Assert.Equal(heldScopes.Length, heldScopes.Distinct().Count());
    }
}
