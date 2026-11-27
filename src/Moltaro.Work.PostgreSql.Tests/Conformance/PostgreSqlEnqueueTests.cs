using Moltaro.Work.PostgreSql.Tests.Database;
using Moltaro.Work.TestSupport.Conformance;

namespace Moltaro.Work.PostgreSql.Tests.Conformance;

// The conformance cases of the area Enqueue, on PostgreSQL.
public sealed class PostgreSqlEnqueueTests : EnqueueConformance
{
    public PostgreSqlEnqueueTests(PostgreSqlDatabase database)
        : base(database)
    {
    }
}
