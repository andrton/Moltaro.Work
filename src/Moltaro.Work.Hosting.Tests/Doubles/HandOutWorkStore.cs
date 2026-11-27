using System.Data.Common;

namespace Moltaro.Work.Hosting.Tests.Doubles;

// The provider of the hosting tests, which touches no database. It records the transactions of its enqueues and hands
// out the claims a test gave it, each once; every fenced write succeeds, and nothing falls due later. With
// ClaimException set, every claim fails with it, as a claim on an unreachable database does.
internal sealed class HandOutWorkStore : IWorkStore
{
    // Guards _claims and Enqueues, which the lanes of a worker and the test reach at once.
    private readonly Lock _lock = new();

    private readonly Queue<WorkClaim> _claims = new();

    private readonly List<DbTransaction> _enqueues = [];

    public Exception? ClaimException { get; set; }

    // The transactions of the enqueues so far, in order.
    public IReadOnlyList<DbTransaction> Enqueues
    {
        get
        {
            lock (_lock)
            {
                return [.. _enqueues];
            }
        }
    }

    // Gives the next claims an item of queue with id itemId, at its first attempt.
    public void HandOut(WorkQueue queue, long itemId)
    {
        var item = new WorkItem(queue, itemId, key: null, scope: null, payload: null, attempt: 1);

        lock (_lock)
        {
            _claims.Enqueue(new WorkClaim(item, fence: 1));
        }
    }

    public Task CheckDatabaseAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task<WorkEnqueueResult> EnqueueAsync(
        DbTransaction transaction,
        WorkQueue queue,
        string? key,
        string? scope,
        string? payload,
        DateTimeOffset? dueAt,
        CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            _enqueues.Add(transaction);

            return Task.FromResult(new WorkEnqueueResult(WorkEnqueueStatusEnum.Added, _enqueues.Count));
        }
    }

    public Task<IReadOnlyList<WorkClaim>> ClaimAsync(
        WorkQueue queue,
        int count,
        TimeSpan lease,
        CancellationToken cancellationToken)
    {
        if (ClaimException is not null)
        {
            return Task.FromException<IReadOnlyList<WorkClaim>>(ClaimException);
        }

        var claims = new List<WorkClaim>();

        lock (_lock)
        {
            while (claims.Count < count && _claims.TryDequeue(out WorkClaim? claim))
            {
                claims.Add(claim);
            }
        }

        return Task.FromResult<IReadOnlyList<WorkClaim>>(claims);
    }

    public Task<IReadOnlyList<WorkClaim>> HeartbeatAsync(
        IReadOnlyList<WorkClaim> claims,
        TimeSpan lease,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<WorkClaim>>([]);
    }

    public Task<bool> CompleteAsync(WorkClaim claim, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    public Task<bool> RetryAsync(WorkClaim claim, TimeSpan delay, string reason, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    public Task<bool> GiveUpAsync(WorkClaim claim, string reason, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    public Task<bool> ReleaseAsync(WorkClaim claim, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    public Task<WorkRequeueStatusEnum> RequeueAsync(WorkQueue queue, long itemId, CancellationToken cancellationToken)
    {
        return Task.FromResult(WorkRequeueStatusEnum.NotDead);
    }

    // Claimable at once while claims are left to hand out; otherwise nothing is due.
    public Task<WorkNextDue> GetNextDueAsync(WorkQueue queue, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            TimeSpan? untilClaimable = _claims.Count > 0 ? TimeSpan.Zero : null;

            return Task.FromResult(new WorkNextDue(untilClaimable, UntilExpired: null));
        }
    }

    public Task<int> DeleteExpiredAsync(WorkQueue queue, int count, CancellationToken cancellationToken)
    {
        return Task.FromResult(0);
    }
}
