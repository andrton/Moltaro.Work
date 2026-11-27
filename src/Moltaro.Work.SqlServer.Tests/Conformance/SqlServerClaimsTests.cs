using Moltaro.Work.SqlServer.Tests.Database;
using Moltaro.Work.TestSupport.Conformance;

namespace Moltaro.Work.SqlServer.Tests.Conformance;

// The conformance cases of the area Claims, on SQL Server.
public sealed class SqlServerClaimsTests : ClaimsConformance
{
    public SqlServerClaimsTests(SqlServerDatabase database)
        : base(database)
    {
    }
}
