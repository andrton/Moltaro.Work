namespace Moltaro.Work.Tests.Enqueue;

// The checks of WorkClient: its constructor, Join and RequeueAsync. Requeueing itself is not implemented yet.
public sealed class WorkClientTests
{
    private static readonly WorkQueue Mail = new("mail", WorkShapeEnum.Log);

    private static readonly CancellationToken Cancelled = new(canceled: true);

    [Fact]
    public async Task A_null_store_is_refused()
    {
        await using var signals = LoopbackSignalHost.Create();

        Assert.Throws<ArgumentNullException>("store", () => new WorkClient(null!, signals));
    }

    [Fact]
    public void Null_signals_are_refused()
    {
        Assert.Throws<ArgumentNullException>("signals", () => new WorkClient(new RecordingWorkStore(), null!));
    }

    [Fact]
    public async Task Join_refuses_a_null_transaction()
    {
        await using var signals = LoopbackSignalHost.Create();
        var client = new WorkClient(new RecordingWorkStore(), signals);

        Assert.Throws<ArgumentNullException>("transaction", () => client.Join(null!));
    }

    [Fact]
    public async Task Join_returns_a_new_transaction_for_every_call_and_does_not_end_the_transaction()
    {
        await using var signals = LoopbackSignalHost.Create();
        var client = new WorkClient(new RecordingWorkStore(), signals);
        var transaction = new FakeDbTransaction(new FakeDbConnection());

        WorkTransaction first = client.Join(transaction);
        WorkTransaction second = client.Join(transaction);

        Assert.NotSame(first, second);
        Assert.NotNull(transaction.Connection);
    }

    [Fact]
    public async Task A_requeue_refuses_a_null_queue_first()
    {
        await using var signals = LoopbackSignalHost.Create();
        var client = new WorkClient(new RecordingWorkStore(), signals);

        Assert.Throws<ArgumentNullException>("queue", () => { _ = client.RequeueAsync(null!, 0, Cancelled); });
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    [InlineData(long.MinValue)]
    public async Task A_requeue_refuses_an_item_id_below_1(long itemId)
    {
        await using var signals = LoopbackSignalHost.Create();
        var client = new WorkClient(new RecordingWorkStore(), signals);

        Assert.Throws<ArgumentOutOfRangeException>("itemId", () => { _ = client.RequeueAsync(Mail, itemId, Cancelled); });
    }

    [Fact]
    public async Task A_requeue_with_a_token_that_is_already_cancelled_gives_a_cancelled_task()
    {
        await using var signals = LoopbackSignalHost.Create();
        var client = new WorkClient(new RecordingWorkStore(), signals);

        Task<WorkRequeueStatusEnum> requeue = client.RequeueAsync(Mail, 1, Cancelled);

        Assert.True(requeue.IsCanceled);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => requeue);
    }

    [Fact]
    public async Task A_requeue_that_passes_its_checks_is_not_supported_yet()
    {
        await using var signals = LoopbackSignalHost.Create();
        var client = new WorkClient(new RecordingWorkStore(), signals);

        Assert.Throws<NotSupportedException>(
            () => { _ = client.RequeueAsync(Mail, 1, TestContext.Current.CancellationToken); });
    }
}
