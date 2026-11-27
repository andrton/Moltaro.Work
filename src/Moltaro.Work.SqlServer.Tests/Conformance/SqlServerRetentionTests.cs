using Moltaro.Work.SqlServer.Tests.Database;
using Moltaro.Work.TestSupport.Conformance;

namespace Moltaro.Work.SqlServer.Tests.Conformance;

// The conformance cases of the area Retention, on SQL Server.
public sealed class SqlServerRetentionTests : RetentionConformance
{
    public SqlServerRetentionTests(SqlServerDatabase database)
        : base(database)
    {
    }
}
