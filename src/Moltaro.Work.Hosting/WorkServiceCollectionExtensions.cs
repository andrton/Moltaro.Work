using Microsoft.Extensions.DependencyInjection;

namespace Moltaro.Work.Hosting;

/// <summary>
/// Registers Moltaro.Work in a service container and a .NET generic host: the store of the provider and a
/// <see cref="WorkClient"/> over it, and a <see cref="WorkWorker"/> per queue with its handler. Not implemented yet.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Moltaro.Signals.ISignals"/> of the container comes from <c>AddSignalHost</c> or
/// <c>AddSignalClient</c> of <c>Moltaro.Signals.Hosting</c>, which the application registers itself. A container
/// serves one store, one client and that one <see cref="Moltaro.Signals.ISignals"/>.
/// </para>
/// <para>
/// The methods change the <see cref="IServiceCollection"/>, which is not safe for concurrent use: an application calls
/// them while it builds its container, as every registration. A <see langword="null"/> argument that is not optional
/// gives <see cref="ArgumentNullException"/>, thrown by the call before anything is registered.
/// </para>
/// <para>
/// The workers write log lines through the container's <see cref="Microsoft.Extensions.Logging.ILoggerFactory"/>. The
/// named values are structured properties; the message text is not part of the contract.
/// </para>
/// <list type="table">
/// <listheader>
/// <term>Category and level</term>
/// <description>When, and its values</description>
/// </listheader>
/// <item>
/// <term><c>Moltaro.Work.WorkWorker</c>, Warning</term>
/// <description>A handler threw; <c>Queue</c>, <c>ItemId</c>, <c>Attempt</c>, and the exception.</description>
/// </item>
/// <item>
/// <term><c>Moltaro.Work.WorkWorker</c>, Error</term>
/// <description>The worker raised <see cref="WorkWorker.Failed"/>; <c>Queue</c>, and the exception.</description>
/// </item>
/// </list>
/// </remarks>
public static class WorkServiceCollectionExtensions
{
    /// <summary>
    /// Registers the store that <paramref name="createStore"/> makes and a <see cref="WorkClient"/> over it and the
    /// container's <see cref="Moltaro.Signals.ISignals"/>, both as singletons. Not implemented yet.
    /// </summary>
    /// <param name="services">The container to register in.</param>
    /// <param name="createStore">
    /// Makes the store of the provider of the application's database, such as a <c>PostgreSqlWorkStore</c> over the
    /// application's data source. It is called once, when the store or the client is first resolved.
    /// </param>
    /// <returns><paramref name="services"/>.</returns>
    /// <remarks>
    /// <para>
    /// The <see cref="IWorkStore"/> is registered as the one object that <paramref name="createStore"/> makes, and the
    /// <see cref="WorkClient"/> over it as one object too, which the application resolves to enqueue in its
    /// transactions and which the workers of <see cref="AddWorkWorker{THandler}"/> use.
    /// </para>
    /// <para>
    /// The client's clock is <see cref="TimeProvider.System"/>, the default clock of the
    /// <see cref="Moltaro.Signals.ISignals"/> that <c>Moltaro.Signals.Hosting</c> 0.1.0 registers, so that it is the
    /// clock of that <see cref="Moltaro.Signals.ISignals"/>, as the constructor of <see cref="WorkClient"/> requires.
    /// An application that gives its <see cref="Moltaro.Signals.ISignals"/> another clock creates the
    /// <see cref="WorkClient"/> itself, with that clock, and registers it instead of calling this method.
    /// </para>
    /// <para>
    /// The <see cref="Moltaro.Signals.ISignals"/> comes from <c>AddSignalHost</c> or <c>AddSignalClient</c> of
    /// <c>Moltaro.Signals.Hosting</c>. A container without one fails when the client is resolved. The application owns
    /// and disposes what the store uses, such as its data source.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown by the call: <paramref name="services"/> or <paramref name="createStore"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown by the call after its argument checks: registering the store and the client is not implemented yet.
    /// </exception>
    public static IServiceCollection AddWork(this IServiceCollection services, Func<IServiceProvider, IWorkStore> createStore)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(createStore);

        throw new NotSupportedException("Registering the store and the client is not implemented yet.");
    }

    /// <summary>
    /// Registers a worker of a queue, which runs from the start of the application until it stops, and the handler of
    /// its items. Not implemented yet.
    /// </summary>
    /// <typeparam name="THandler">
    /// The handler of the queue's items, resolved from the container for every item.
    /// </typeparam>
    /// <param name="services">The container to register in.</param>
    /// <param name="queue">The queue whose items the worker handles.</param>
    /// <param name="configure">
    /// Fills the options of the worker when the worker is created; <see langword="null"/> keeps the defaults of
    /// <see cref="WorkWorkerOptions"/>.
    /// </param>
    /// <returns><paramref name="services"/>.</returns>
    /// <remarks>
    /// <para>
    /// It registers <typeparamref name="THandler"/> as scoped, unless it is registered already, and a hosted service.
    /// When the hosted service is resolved, it creates a <see cref="WorkWorker"/> of <paramref name="queue"/> over the
    /// container's <see cref="WorkClient"/>, whether <see cref="AddWork"/> or the application itself registered it,
    /// with the options that <paramref name="configure"/> fills. A container without a <see cref="WorkClient"/>, and
    /// options that the constructor of <see cref="WorkWorker"/> refuses, fail the start of the application.
    /// </para>
    /// <para>
    /// For every item the worker creates a scope of its own and resolves <typeparamref name="THandler"/> in it, so that
    /// the handler and its scoped services serve that one item.
    /// </para>
    /// <para>
    /// The worker runs from the start of the application until the application stops. When it stops, the worker stops
    /// as <see cref="WorkWorker.RunAsync"/> says: it cancels the token of every running handler and waits for each,
    /// bounded by the host's shutdown timeout.
    /// </para>
    /// <para>
    /// It writes the log lines that the remarks of <see cref="WorkServiceCollectionExtensions"/> name.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown by the call: <paramref name="services"/> or <paramref name="queue"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown by the call: <paramref name="services"/> already has a worker of a queue with the name of
    /// <paramref name="queue"/>.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown by the call after its argument checks: registering a worker is not implemented yet.
    /// </exception>
    public static IServiceCollection AddWorkWorker<THandler>(
        this IServiceCollection services,
        WorkQueue queue,
        Action<WorkWorkerOptions>? configure = null)
        where THandler : class, IWorkHandler
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(queue);

        throw new NotSupportedException("Registering a worker is not implemented yet.");
    }
}
