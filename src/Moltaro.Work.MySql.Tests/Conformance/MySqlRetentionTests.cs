using Moltaro.Work.MySql.Tests.Database;
using Moltaro.Work.TestSupport.Conformance;

namespace Moltaro.Work.MySql.Tests.Conformance;

// The conformance cases of the area Retention, on MySQL.
public sealed class MySqlRetentionTests : RetentionConformance
{
    public MySqlRetentionTests(MySqlDatabase database)
        : base(database)
    {
    }
}
