namespace Moltaro.Work.Hosting.Tests.Doubles;

// One item as its handler received it: the provider of the scope the handler was resolved in, and its token.
internal sealed record HandledItem(WorkItem Item, IServiceProvider Services, CancellationToken Token);
