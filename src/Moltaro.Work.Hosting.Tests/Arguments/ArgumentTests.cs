using Microsoft.Extensions.DependencyInjection;
using Moltaro.Work.Hosting.Tests.Doubles;

namespace Moltaro.Work.Hosting.Tests.Arguments;

// The checks of AddWork and AddWorkWorker. Registering is not implemented yet, so a call that passes the checks throws
// NotSupportedException, and registers nothing.
public sealed class ArgumentTests
{
    private static readonly WorkQueue Balances = new("balances", WorkShapeEnum.Coalescing);

    [Fact]
    public void AddWork_refuses_null_services()
    {
        Assert.Throws<ArgumentNullException>(
            "services",
            () => WorkServiceCollectionExtensions.AddWork(null!, _ => new HandOutWorkStore()));
    }

    [Fact]
    public void AddWork_refuses_a_null_store_factory()
    {
        Assert.Throws<ArgumentNullException>("createStore", () => new ServiceCollection().AddWork(null!));
    }

    [Fact]
    public void AddWork_that_passes_its_checks_is_not_supported_yet()
    {
        var services = new ServiceCollection();

        Assert.Throws<NotSupportedException>(() => services.AddWork(_ => new HandOutWorkStore()));
        Assert.Empty(services);
    }

    [Fact]
    public void AddWorkWorker_refuses_null_services()
    {
        Assert.Throws<ArgumentNullException>(
            "services",
            () => WorkServiceCollectionExtensions.AddWorkWorker<CompletingHandler>(null!, Balances));
    }

    [Fact]
    public void AddWorkWorker_refuses_a_null_queue()
    {
        Assert.Throws<ArgumentNullException>(
            "queue",
            () => new ServiceCollection().AddWorkWorker<CompletingHandler>(null!));
    }

    // The options are optional: without them the call passes its checks.
    [Fact]
    public void AddWorkWorker_that_passes_its_checks_is_not_supported_yet()
    {
        var services = new ServiceCollection();

        Assert.Throws<NotSupportedException>(() => services.AddWorkWorker<CompletingHandler>(Balances));
        Assert.Empty(services);
    }
}
