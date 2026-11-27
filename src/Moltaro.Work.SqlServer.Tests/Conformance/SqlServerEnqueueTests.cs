using Moltaro.Work.SqlServer.Tests.Database;
using Moltaro.Work.TestSupport.Conformance;

namespace Moltaro.Work.SqlServer.Tests.Conformance;

// The conformance cases of the area Enqueue, on SQL Server.
public sealed class SqlServerEnqueueTests : EnqueueConformance
{
    public SqlServerEnqueueTests(SqlServerDatabase database)
        : base(database)
    {
    }
}
