using Moltaro.Work.MySql.Tests.Database;
using Moltaro.Work.TestSupport.Conformance;

namespace Moltaro.Work.MySql.Tests.Conformance;

// The conformance cases of the area Enqueue, on MySQL.
public sealed class MySqlEnqueueTests : EnqueueConformance
{
    public MySqlEnqueueTests(MySqlDatabase database)
        : base(database)
    {
    }
}
