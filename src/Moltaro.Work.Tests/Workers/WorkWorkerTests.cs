using Moltaro.Signals;
using Moltaro.Work.Tests.Enqueue;

namespace Moltaro.Work.Tests.Workers;

// The checks of the constructor of WorkWorker and of RunAsync. Running a worker is not implemented yet, so a run that
// passes the checks throws NotSupportedException.
public sealed class WorkWorkerTests : IAsyncDisposable
{
    private static readonly WorkQueue Balances = new("balances", WorkShapeEnum.Coalescing);

    private readonly SignalHost _signals = LoopbackSignalHost.Create();

    private readonly WorkClient _client;

    public WorkWorkerTests()
    {
        _client = new WorkClient(new RecordingWorkStore(), _signals);
    }

    public ValueTask DisposeAsync()
    {
        return _signals.DisposeAsync();
    }

    [Fact]
    public void A_null_client_is_refused()
    {
        Assert.Throws<ArgumentNullException>("client", () => new WorkWorker(null!, Balances, new CompletingHandler()));
    }

    [Fact]
    public void A_null_queue_is_refused()
    {
        Assert.Throws<ArgumentNullException>("queue", () => new WorkWorker(_client, null!, new CompletingHandler()));
    }

    [Fact]
    public void A_null_handler_is_refused()
    {
        Assert.Throws<ArgumentNullException>("handler", () => new WorkWorker(_client, Balances, null!));
    }

    [Fact]
    public void A_worker_keeps_its_queue()
    {
        var worker = new WorkWorker(_client, Balances, new CompletingHandler());

        Assert.Same(Balances, worker.Queue);
    }

    [Fact]
    public void A_run_that_passes_its_checks_is_not_supported_yet()
    {
        var worker = new WorkWorker(_client, Balances, new CompletingHandler());

        Assert.Throws<NotSupportedException>(() => { _ = worker.RunAsync(TestContext.Current.CancellationToken); });
    }

    [Fact]
    public async Task A_run_whose_token_is_already_cancelled_gives_a_cancelled_task()
    {
        var worker = new WorkWorker(_client, Balances, new CompletingHandler());

        Task run = worker.RunAsync(new CancellationToken(canceled: true));

        Assert.True(run.IsCanceled);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
    }

    [Fact]
    public void A_second_run_is_refused()
    {
        var worker = new WorkWorker(_client, Balances, new CompletingHandler());
        Assert.Throws<NotSupportedException>(() => { _ = worker.RunAsync(TestContext.Current.CancellationToken); });

        Assert.Throws<InvalidOperationException>(() => { _ = worker.RunAsync(TestContext.Current.CancellationToken); });
    }
}
