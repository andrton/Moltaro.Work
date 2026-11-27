using Microsoft.Extensions.DependencyInjection;
using Moltaro.Signals;
using Moltaro.Signals.Hosting;
using Moltaro.Work.Hosting.Tests.Doubles;
using Moltaro.Work.TestSupport;
using Moltaro.Work.TestSupport.Doubles;

namespace Moltaro.Work.Hosting.Tests.Registration;

// What AddWork registers: one store, made once, and one client over it and the container's ISignals.
public sealed class AddWorkTests
{
    private static readonly WorkQueue Balances = new("balances", WorkShapeEnum.Coalescing);

    [Fact(Skip = SkipReason.NotImplemented)]
    public async Task The_client_resolves_over_the_registered_store_and_signals()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var store = new HandOutWorkStore();
        int storesMade = 0;
        var services = new ServiceCollection();
        services.AddSignalHost(LoopbackSignals.Configure);
        services.AddWork(_ =>
        {
            storesMade++;

            return store;
        });
        await using ServiceProvider provider = services.BuildServiceProvider();

        WorkClient client = provider.GetRequiredService<WorkClient>();

        Assert.Same(client, provider.GetRequiredService<WorkClient>());
        Assert.Same(store, provider.GetRequiredService<IWorkStore>());
        Assert.Equal(1, storesMade);

        // A member of the queue's signal, as a worker joins it (W11).
        ISignals signals = provider.GetRequiredService<ISignals>();
        using ISignalSubscription member = signals.Subscribe("moltaro.work.balances", "moltaro.work");
        Assert.Equal(SignalWaitResultEnum.Joined, await member.WaitAsync(until: null, cancellationToken));

        var transaction = new FakeDbTransaction(new FakeDbConnection());
        WorkTransaction work = client.Join(transaction);
        await work.EnqueueAsync(Balances, key: "account-7", cancellationToken: cancellationToken);
        await work.CommitAsync(cancellationToken);

        Assert.Same(transaction, Assert.Single(store.Enqueues));
        Assert.Equal(SignalWaitResultEnum.Signaled, await member.WaitAsync(until: null, cancellationToken));
    }
}
