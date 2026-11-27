using System.Data;
using System.Data.Common;

namespace Moltaro.Work.Tests.Enqueue;

// A transaction that touches no database. As an ADO.NET transaction does, it loses its connection when it ends, by a
// commit or a rollback; that is how the core sees that a transaction has ended.
internal sealed class FakeDbTransaction : DbTransaction
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
