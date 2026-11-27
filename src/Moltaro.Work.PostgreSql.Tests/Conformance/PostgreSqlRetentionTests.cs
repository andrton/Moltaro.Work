using Moltaro.Work.PostgreSql.Tests.Database;
using Moltaro.Work.TestSupport.Conformance;

namespace Moltaro.Work.PostgreSql.Tests.Conformance;

// The conformance cases of the area Retention, on PostgreSQL.
public sealed class PostgreSqlRetentionTests : RetentionConformance
{
    public PostgreSqlRetentionTests(PostgreSqlDatabase database)
        : base(database)
    {
    }
}
