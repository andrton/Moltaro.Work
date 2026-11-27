using Moltaro.Work.PostgreSql.Tests.Database;
using Moltaro.Work.TestSupport.Conformance;

namespace Moltaro.Work.PostgreSql.Tests.Conformance;

// The conformance cases of the area Retries, on PostgreSQL.
public sealed class PostgreSqlRetriesTests : RetriesConformance
{
    public PostgreSqlRetriesTests(PostgreSqlDatabase database)
        : base(database)
    {
    }
}
