using System.Data.Common;

namespace Moltaro.Work.TestSupport.Doubles;

// A data source of another database than any provider's: its connections are FakeDbConnection, which touch no
// database. It is what the constructor of a provider's store refuses.
public sealed class FakeDbDataSource : DbDataSource
{
    public override string ConnectionString => throw new InvalidOperationException("This data source of the tests touches no database.");

    protected override DbConnection CreateDbConnection()
    {
        return new FakeDbConnection();
    }
}
