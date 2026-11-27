using System.Data;
using System.Data.Common;

namespace Moltaro.Work.TestSupport.Doubles;

// A connection that touches no database, so every member throws: the caller's connection in the tests of the core,
// which only hands it to the store, and the connection of FakeDbDataSource, whose type a provider refuses.
public sealed class FakeDbConnection : DbConnection
{
    [System.Diagnostics.CodeAnalysis.AllowNull]
    public override string ConnectionString
    {
        get => throw Untouched();
        set => throw Untouched();
    }

    public override string Database => throw Untouched();

    public override string DataSource => throw Untouched();

    public override string ServerVersion => throw Untouched();

    public override ConnectionState State => throw Untouched();

    public override void ChangeDatabase(string databaseName)
    {
        throw Untouched();
    }

    public override void Close()
    {
        throw Untouched();
    }

    public override void Open()
    {
        throw Untouched();
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        throw Untouched();
    }

    protected override DbCommand CreateDbCommand()
    {
        throw Untouched();
    }

    private static InvalidOperationException Untouched()
    {
        return new InvalidOperationException("This connection of the tests touches no database.");
    }
}
