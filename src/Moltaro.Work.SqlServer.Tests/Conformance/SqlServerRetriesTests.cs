using Moltaro.Work.SqlServer.Tests.Database;
using Moltaro.Work.TestSupport.Conformance;

namespace Moltaro.Work.SqlServer.Tests.Conformance;

// The conformance cases of the area Retries, on SQL Server.
public sealed class SqlServerRetriesTests : RetriesConformance
{
    public SqlServerRetriesTests(SqlServerDatabase database)
        : base(database)
    {
    }
}
