using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Moltaro.Work.SqlServer.Tests.Database;
using Moltaro.Work.TestSupport.Conformance;

[assembly: AssemblyFixture(typeof(SqlServerDatabase))]

namespace Moltaro.Work.SqlServer.Tests.Database;

// The database of one test run: created empty on the server that MOLTARO_WORK_SQLSERVER names, with
// READ_COMMITTED_SNAPSHOT ON as the store needs it, given the schema script, shared by every test of the run and dropped
// at its end. Without the variable every test fails, because a provider is never tested without its database.
public sealed class SqlServerDatabase : ConformanceDatabase, IAsyncLifetime
{
    private const string VariableName = "MOLTARO_WORK_SQLSERVER";

    private readonly string _name = "moltaro_work_test_" + Guid.NewGuid().ToString("N");

    // The connection string of the server, to create and drop the database; null until the run has created it.
    private string? _serverConnectionString;

    private DbDataSource? _dataSource;

    private DbDataSource DataSource =>
        _dataSource ?? throw new InvalidOperationException("The database of the run has not been created.");

    public async ValueTask InitializeAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string? serverConnectionString = Environment.GetEnvironmentVariable(VariableName);

        if (string.IsNullOrEmpty(serverConnectionString))
        {
            throw new InvalidOperationException(
                $"Set {VariableName} to the connection string of a SQL Server 2019 or later on which the tests may create a database.");
        }

        await using (var server = new SqlConnection(serverConnectionString))
        {
            await server.OpenAsync(cancellationToken);

            await using var create = new SqlCommand(
                $"CREATE DATABASE [{_name}]; ALTER DATABASE [{_name}] SET READ_COMMITTED_SNAPSHOT ON;",
                server);
            await create.ExecuteNonQueryAsync(cancellationToken);
        }

        _serverConnectionString = serverConnectionString;

        var builder = new SqlConnectionStringBuilder(serverConnectionString) { InitialCatalog = _name };
        _dataSource = SqlClientFactory.Instance.CreateDataSource(builder.ConnectionString);

        // The script runs in one transaction, so that a failure leaves no half of the schema behind.
        await using DbConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using DbCommand apply = connection.CreateCommand();
        apply.Transaction = transaction;
        apply.CommandText = SqlServerWorkStore.SchemaScript;
        await apply.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_dataSource is not null)
        {
            await _dataSource.DisposeAsync();
        }

        if (_serverConnectionString is null)
        {
            return;
        }

        await using var server = new SqlConnection(_serverConnectionString);
        await server.OpenAsync();

        // The pool still holds connections to the database; SINGLE_USER closes them, so that the drop does not wait.
        await using var drop = new SqlCommand(
            $"ALTER DATABASE [{_name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_name}];",
            server);
        await drop.ExecuteNonQueryAsync();
    }

    public override IWorkStore CreateStore()
    {
        return new SqlServerWorkStore(DataSource);
    }

    public override async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        return await DataSource.OpenConnectionAsync(cancellationToken);
    }

    // The one statement of this project that moves the database's clock: every time of the queue's items, NULL
    // staying NULL, is shifted back. DATEADD takes an int, which holds the microseconds of about 35 minutes only, so the
    // shift is given as whole seconds and the microseconds left over.
    public override async Task MoveClockAsync(WorkQueue queue, TimeSpan by, CancellationToken cancellationToken)
    {
        await using DbCommand command = DataSource.CreateCommand(
            """
            UPDATE moltaro_work_item
            SET due_at = DATEADD(microsecond, -@microseconds, DATEADD(second, -@seconds, due_at)),
                again_due_at = DATEADD(microsecond, -@microseconds, DATEADD(second, -@seconds, again_due_at)),
                lease_until = DATEADD(microsecond, -@microseconds, DATEADD(second, -@seconds, lease_until)),
                enqueued_at = DATEADD(microsecond, -@microseconds, DATEADD(second, -@seconds, enqueued_at)),
                claimed_at = DATEADD(microsecond, -@microseconds, DATEADD(second, -@seconds, claimed_at)),
                finished_at = DATEADD(microsecond, -@microseconds, DATEADD(second, -@seconds, finished_at))
            WHERE queue = @queue
            """);
        command.Parameters.Add(new SqlParameter("@seconds", SqlDbType.Int) { Value = checked((int)(by.Ticks / TimeSpan.TicksPerSecond)) });
        command.Parameters.Add(new SqlParameter("@microseconds", SqlDbType.Int) { Value = (int)(by.Ticks % TimeSpan.TicksPerSecond / TimeSpan.TicksPerMicrosecond) });
        command.Parameters.Add(new SqlParameter("@queue", SqlDbType.VarChar, 100) { Value = queue.Name });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
