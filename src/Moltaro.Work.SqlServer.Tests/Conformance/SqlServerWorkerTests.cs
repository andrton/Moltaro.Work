using Moltaro.Work.SqlServer.Tests.Database;
using Moltaro.Work.TestSupport.Conformance;

namespace Moltaro.Work.SqlServer.Tests.Conformance;

// The conformance cases of the area Worker, on SQL Server.
public sealed class SqlServerWorkerTests : WorkerConformance
{
    public SqlServerWorkerTests(SqlServerDatabase database)
        : base(database)
    {
    }
}
