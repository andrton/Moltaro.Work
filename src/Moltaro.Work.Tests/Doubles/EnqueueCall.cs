using System.Data.Common;

namespace Moltaro.Work.Tests.Doubles;

// The arguments of one call of IWorkStore.EnqueueAsync, as the store received them.
internal sealed record EnqueueCall(
    DbTransaction Transaction,
    WorkQueue Queue,
    string? Key,
    string? Scope,
    string? Payload,
    DateTimeOffset? DueAt,
    CancellationToken CancellationToken);
