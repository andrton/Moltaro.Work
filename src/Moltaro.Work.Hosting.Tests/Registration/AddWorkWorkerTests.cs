using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moltaro.Signals;
using Moltaro.Signals.Hosting;
using Moltaro.Work.Hosting.Tests.Doubles;
using Moltaro.Work.TestSupport;

namespace Moltaro.Work.Hosting.Tests.Registration;

// What AddWorkWorker registers: a worker of the queue in the generic host, over the container's client, with a scope
// per item, stopped with the application, and its log lines.
public sealed class AddWorkWorkerTests
{
    private const string WorkerCategory = "Moltaro.Work.WorkWorker";

    private static readonly WorkQueue Balances = new("balances", WorkShapeEnum.Coalescing);

    [Fact(Skip = SkipReason.NotImplemented)]
    public async Task A_worker_uses_a_client_that_the_application_registered_without_AddWork()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var store = new HandOutWorkStore();
        store.HandOut(Balances, itemId: 7);
        var handled = new HandledItems();
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(settings: null);
        builder.Services.AddSignalHost(LoopbackSignals.Configure);
        builder.Services.AddSingleton(provider => new WorkClient(store, provider.GetRequiredService<ISignals>()));
        builder.Services.AddSingleton(handled);
        builder.Services.AddWorkWorker<CompletingHandler>(Balances);
        using IHost host = builder.Build();

        await host.StartAsync(cancellationToken);
        HandledItem item = await handled.NextStartedAsync(cancellationToken);
        await host.StopAsync(cancellationToken);

        Assert.Equal(7, item.Item.Id);
    }

    [Fact(Skip = SkipReason.NotImplemented)]
    public async Task A_handler_runs_in_a_scope_of_its_own_for_every_item()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var store = new HandOutWorkStore();
        store.HandOut(Balances, itemId: 7);
        store.HandOut(Balances, itemId: 8);
        var handled = new HandledItems();
        HostApplicationBuilder builder = CreateBuilder(store, handled);
        builder.Services.AddWorkWorker<CompletingHandler>(Balances);
        using IHost host = builder.Build();

        await host.StartAsync(cancellationToken);
        HandledItem first = await handled.NextStartedAsync(cancellationToken);
        HandledItem second = await handled.NextStartedAsync(cancellationToken);
        await host.StopAsync(cancellationToken);

        Assert.Equal([7L, 8L], [first.Item.Id, second.Item.Id]);
        Assert.NotSame(first.Services, second.Services);
        Assert.NotSame(host.Services, first.Services);
        Assert.NotSame(host.Services, second.Services);
    }

    [Fact(Skip = SkipReason.NotImplemented)]
    public async Task A_worker_stops_with_the_application_after_its_handlers_ended()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var store = new HandOutWorkStore();
        store.HandOut(Balances, itemId: 7);
        var handled = new HandledItems();
        HostApplicationBuilder builder = CreateBuilder(store, handled);
        builder.Services.AddWorkWorker<WaitingHandler>(Balances);
        using IHost host = builder.Build();
        await host.StartAsync(cancellationToken);
        HandledItem running = await handled.NextStartedAsync(cancellationToken);

        await host.StopAsync(cancellationToken);

        Assert.True(running.Token.IsCancellationRequested);
        Assert.True(handled.HasEnded(7));
    }

    [Fact(Skip = SkipReason.NotImplemented)]
    public void A_second_worker_of_one_queue_is_refused()
    {
        var services = new ServiceCollection();
        services.AddWorkWorker<CompletingHandler>(Balances);

        // Another declaration of the same queue name is the same queue.
        var sameName = new WorkQueue("balances", WorkShapeEnum.Coalescing);

        Assert.Throws<ArgumentException>("queue", () => services.AddWorkWorker<WaitingHandler>(sameName));
    }

    [Fact(Skip = SkipReason.NotImplemented)]
    public async Task A_handler_that_threw_is_logged_as_a_warning_with_its_queue_item_and_attempt()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var store = new HandOutWorkStore();
        store.HandOut(Balances, itemId: 7);
        var logs = new RecordingLoggerProvider(WorkerCategory, LogLevel.Warning);
        HostApplicationBuilder builder = CreateBuilder(store, new HandledItems());
        builder.Logging.AddProvider(logs);
        builder.Services.AddWorkWorker<ThrowingHandler>(Balances);
        using IHost host = builder.Build();

        await host.StartAsync(cancellationToken);
        LogEntry entry = await logs.Entry.WaitAsync(cancellationToken);
        await host.StopAsync(cancellationToken);

        Assert.Equal("balances", entry.Values["Queue"]);
        Assert.Equal(7L, entry.Values["ItemId"]);
        Assert.Equal(1, entry.Values["Attempt"]);
        Assert.Same(ThrowingHandler.Failure, entry.Exception);
    }

    [Fact(Skip = SkipReason.NotImplemented)]
    public async Task A_failure_of_the_worker_is_logged_as_an_error_with_its_queue()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var failure = new UnreachableDatabaseException();
        var store = new HandOutWorkStore { ClaimException = failure };
        var logs = new RecordingLoggerProvider(WorkerCategory, LogLevel.Error);
        HostApplicationBuilder builder = CreateBuilder(store, new HandledItems());
        builder.Logging.AddProvider(logs);
        builder.Services.AddWorkWorker<CompletingHandler>(Balances);
        using IHost host = builder.Build();

        await host.StartAsync(cancellationToken);
        LogEntry entry = await logs.Entry.WaitAsync(cancellationToken);
        await host.StopAsync(cancellationToken);

        Assert.Equal("balances", entry.Values["Queue"]);
        Assert.Same(failure, entry.Exception);
    }

    // A generic host with the signals of the process, the store through AddWork, and what the handlers saw.
    private static HostApplicationBuilder CreateBuilder(HandOutWorkStore store, HandledItems handled)
    {
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(settings: null);
        builder.Services.AddSignalHost(LoopbackSignals.Configure);
        builder.Services.AddWork(_ => store);
        builder.Services.AddSingleton(handled);

        return builder;
    }
}
