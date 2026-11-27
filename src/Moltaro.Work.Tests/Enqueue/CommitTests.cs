using Moltaro.Signals;

namespace Moltaro.Work.Tests.Enqueue;

// The checks of WorkTransaction.CommitAsync. Committing itself is not implemented yet, so a call that passes the checks
// throws NotSupportedException.
public sealed class CommitTests : IAsyncDisposable
{
    private static readonly WorkQueue Balances = new("balances", WorkShapeEnum.Coalescing);

    private static readonly CancellationToken Cancelled = new(canceled: true);

    private readonly SignalHost _signals = LoopbackSignalHost.Create();

    private readonly RecordingWorkStore _store = new();

    private readonly WorkTransaction _transaction;

    public CommitTests()
    {
        _transaction = new WorkClient(_store, _signals).Join(new FakeDbTransaction(new FakeDbConnection()));
    }

    public ValueTask DisposeAsync()
    {
        return _signals.DisposeAsync();
    }

    [Fact]
    public void A_commit_that_passes_its_checks_is_not_supported_yet()
    {
        Assert.Throws<NotSupportedException>(() => { _ = _transaction.CommitAsync(TestContext.Current.CancellationToken); });
    }

    [Fact]
    public async Task A_token_that_is_already_cancelled_gives_a_cancelled_task()
    {
        Task commit = _transaction.CommitAsync(Cancelled);

        Assert.True(commit.IsCanceled);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => commit);
    }

    [Fact]
    public void A_second_commit_is_refused_whatever_the_outcome_of_the_first()
    {
        _ = _transaction.CommitAsync(Cancelled);

        Assert.Throws<InvalidOperationException>(() => { _ = _transaction.CommitAsync(Cancelled); });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_commit_after_an_enqueue_whose_task_faulted_or_was_cancelled_is_refused(bool cancelled)
    {
        _store.EnqueueResult = cancelled
            ? Task.FromCanceled<WorkEnqueueResult>(Cancelled)
            : Task.FromException<WorkEnqueueResult>(new TimeoutException("The database did not answer."));

        Task<WorkEnqueueResult> enqueue = _transaction.EnqueueAsync(
            Balances,
            key: "invoice-7",
            cancellationToken: TestContext.Current.CancellationToken);
        await Assert.ThrowsAnyAsync<Exception>(() => enqueue);

        Assert.Throws<InvalidOperationException>(() => { _ = _transaction.CommitAsync(Cancelled); });
    }
}
