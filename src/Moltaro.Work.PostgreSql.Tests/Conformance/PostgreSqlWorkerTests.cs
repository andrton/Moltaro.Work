using Moltaro.Work.PostgreSql.Tests.Database;
using Moltaro.Work.TestSupport.Conformance;

namespace Moltaro.Work.PostgreSql.Tests.Conformance;

// The conformance cases of the area Worker, on PostgreSQL.
public sealed class PostgreSqlWorkerTests : WorkerConformance
{
    public PostgreSqlWorkerTests(PostgreSqlDatabase database)
        : base(database)
    {
    }
}
