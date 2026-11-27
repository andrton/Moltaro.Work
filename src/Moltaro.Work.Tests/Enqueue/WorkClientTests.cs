using Moltaro.Work.Tests.Doubles;

namespace Moltaro.Work.Tests.Enqueue;

// The checks of WorkClient: its constructor and Join.
public sealed class WorkClientTests
{
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
}
