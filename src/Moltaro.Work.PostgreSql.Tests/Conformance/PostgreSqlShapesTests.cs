using Moltaro.Work.PostgreSql.Tests.Database;
using Moltaro.Work.TestSupport.Conformance;

namespace Moltaro.Work.PostgreSql.Tests.Conformance;

// The conformance cases of the area Shapes, on PostgreSQL.
public sealed class PostgreSqlShapesTests : ShapesConformance
{
    public PostgreSqlShapesTests(PostgreSqlDatabase database)
        : base(database)
    {
    }
}
