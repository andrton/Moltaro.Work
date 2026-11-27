using Moltaro.Work.PostgreSql.Tests.Database;
using Moltaro.Work.TestSupport.Conformance;

namespace Moltaro.Work.PostgreSql.Tests.Conformance;

// The conformance cases of the area Scopes, on PostgreSQL.
public sealed class PostgreSqlScopesTests : ScopesConformance
{
    public PostgreSqlScopesTests(PostgreSqlDatabase database)
        : base(database)
    {
    }
}
