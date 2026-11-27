using Moltaro.Work.MySql.Tests.Database;
using Moltaro.Work.TestSupport.Conformance;

namespace Moltaro.Work.MySql.Tests.Conformance;

// The conformance cases of the area Scopes, on MySQL.
public sealed class MySqlScopesTests : ScopesConformance
{
    public MySqlScopesTests(MySqlDatabase database)
        : base(database)
    {
    }
}
