using Moltaro.Work.SqlServer.Tests.Database;
using Moltaro.Work.TestSupport.Conformance;

namespace Moltaro.Work.SqlServer.Tests.Conformance;

// The conformance cases of the area Shapes, on SQL Server.
public sealed class SqlServerShapesTests : ShapesConformance
{
    public SqlServerShapesTests(SqlServerDatabase database)
        : base(database)
    {
    }
}
