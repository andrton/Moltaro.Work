using Moltaro.Work.PostgreSql.Tests.Database;
using Moltaro.Work.TestSupport.Conformance;

namespace Moltaro.Work.PostgreSql.Tests.Conformance;

// The conformance cases of the area Claims, on PostgreSQL.
public sealed class PostgreSqlClaimsTests : ClaimsConformance
{
    public PostgreSqlClaimsTests(PostgreSqlDatabase database)
        : base(database)
    {
    }
}
