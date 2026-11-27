using Moltaro.Work.MySql.Tests.Database;
using Moltaro.Work.TestSupport.Conformance;

namespace Moltaro.Work.MySql.Tests.Conformance;

// The conformance cases of the area Worker, on MySQL.
public sealed class MySqlWorkerTests : WorkerConformance
{
    public MySqlWorkerTests(MySqlDatabase database)
        : base(database)
    {
    }
}
