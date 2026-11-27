using System.Data;
using System.Data.Common;

namespace Moltaro.Work.TestSupport.Doubles;

// A caller's transaction that touches no database: the transaction of the tests of the core and of the hosting
// package, and the transaction of another database that every provider's store refuses. As an ADO.NET transaction
// does, it loses its connection when it ends, by a commit or a rollback; that is how the core sees that a transaction
// has ended.
public sealed class FakeDbTransaction : DbTransaction
{
    private DbConnection? _connection;

    public FakeDbTransaction(DbConnection connection)
    {
        _connection = connection;
    }

    public override IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;

    protected override DbConnection? DbConnection => _connection;

    public override void Commit()
    {
        _connection = null;
    }

    public override void Rollback()
    {
        _connection = null;
    }
}
