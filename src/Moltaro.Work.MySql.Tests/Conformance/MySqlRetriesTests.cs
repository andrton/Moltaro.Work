using Moltaro.Work.MySql.Tests.Database;
using Moltaro.Work.TestSupport.Conformance;

namespace Moltaro.Work.MySql.Tests.Conformance;

// The conformance cases of the area Retries, on MySQL.
public sealed class MySqlRetriesTests : RetriesConformance
{
    public MySqlRetriesTests(MySqlDatabase database)
        : base(database)
    {
    }
}
