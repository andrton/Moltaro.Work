using Moltaro.Work.MySql.Tests.Database;
using Moltaro.Work.TestSupport.Conformance;

namespace Moltaro.Work.MySql.Tests.Conformance;

// The conformance cases of the area Claims, on MySQL.
public sealed class MySqlClaimsTests : ClaimsConformance
{
    public MySqlClaimsTests(MySqlDatabase database)
        : base(database)
    {
    }
}
