using System.Data;
using System.Data.Common;

namespace Moltaro.Work.Tests.Doubles;

// The caller's connection in every area of these tests. It touches no database: the core only hands it to the store,
// so every member throws.
internal sealed class FakeDbConnection : DbConnection
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
        return new InvalidOperationException("The core does not use the caller's connection itself.");
    }
}
