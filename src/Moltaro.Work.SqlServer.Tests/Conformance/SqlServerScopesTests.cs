using Moltaro.Work.SqlServer.Tests.Database;
using Moltaro.Work.TestSupport.Conformance;

namespace Moltaro.Work.SqlServer.Tests.Conformance;

// The conformance cases of the area Scopes, on SQL Server.
public sealed class SqlServerScopesTests : ScopesConformance
{
    public SqlServerScopesTests(SqlServerDatabase database)
        : base(database)
    {
    }
}
