using System.Data.Common;

namespace Moltaro.Work.Tests.Doubles;

// The provider of every area of these tests: it records its enqueues and answers each with EnqueueResult. Every other
// operation throws, because no behavior that reaches one is implemented yet.
internal sealed class RecordingWorkStore : IWorkStore
{
    public List<EnqueueCall> EnqueueCalls { get; } = [];

    // The task every enqueue returns: a result, or a faulted or cancelled task.
    public Task<WorkEnqueueResult> EnqueueResult { get; set; } =
        Task.FromResult(new WorkEnqueueResult(WorkEnqueueStatusEnum.Added, 1));

    public Task<WorkEnqueueResult> EnqueueAsync(
        DbTransaction transaction,
        WorkQueue queue,
        string? key,
        string? scope,
        string? payload,
        DateTimeOffset? dueAt,
        CancellationToken cancellationToken)
    {
        EnqueueCalls.Add(new EnqueueCall(transaction, queue, key, scope, payload, dueAt, cancellationToken));

        return EnqueueResult;
    }

    public Task CheckDatabaseAsync(CancellationToken cancellationToken)
    {
        throw NotExpected();
    }

    public Task<IReadOnlyList<WorkClaim>> ClaimAsync(
        WorkQueue queue,
        int count,
        TimeSpan lease,
        CancellationToken cancellationToken)
    {
        throw NotExpected();
    }

    public Task<IReadOnlyList<WorkClaim>> HeartbeatAsync(
        IReadOnlyList<WorkClaim> claims,
        TimeSpan lease,
        CancellationToken cancellationToken)
    {
        throw NotExpected();
    }

    public Task<bool> CompleteAsync(WorkClaim claim, CancellationToken cancellationToken)
    {
        throw NotExpected();
    }

    public Task<bool> RetryAsync(WorkClaim claim, TimeSpan delay, string reason, CancellationToken cancellationToken)
    {
        throw NotExpected();
    }

    public Task<bool> GiveUpAsync(WorkClaim claim, string reason, CancellationToken cancellationToken)
    {
        throw NotExpected();
    }

    public Task<bool> ReleaseAsync(WorkClaim claim, CancellationToken cancellationToken)
    {
        throw NotExpected();
    }

    public Task<WorkRequeueStatusEnum> RequeueAsync(WorkQueue queue, long itemId, CancellationToken cancellationToken)
    {
        throw NotExpected();
    }

    public Task<WorkNextDue> GetNextDueAsync(WorkQueue queue, CancellationToken cancellationToken)
    {
        throw NotExpected();
    }

    public Task<int> DeleteExpiredAsync(WorkQueue queue, int count, CancellationToken cancellationToken)
    {
        throw NotExpected();
    }

    private static InvalidOperationException NotExpected()
    {
        return new InvalidOperationException("No test reaches this operation of the store.");
    }
}
