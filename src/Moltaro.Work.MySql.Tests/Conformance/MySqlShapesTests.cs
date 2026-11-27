using Moltaro.Work.MySql.Tests.Database;
using Moltaro.Work.TestSupport.Conformance;

namespace Moltaro.Work.MySql.Tests.Conformance;

// The conformance cases of the area Shapes, on MySQL.
public sealed class MySqlShapesTests : ShapesConformance
{
    public MySqlShapesTests(MySqlDatabase database)
        : base(database)
    {
    }
}
