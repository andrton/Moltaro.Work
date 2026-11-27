using System.Data.Common;
using Moltaro.Work.MySql.Tests.Database;
using Moltaro.Work.TestSupport.Conformance;
using MySqlConnector;

[assembly: AssemblyFixture(typeof(MySqlDatabase))]

namespace Moltaro.Work.MySql.Tests.Database;

// The database of one test run: created empty on the server that MOLTARO_WORK_MYSQL names, given the schema script,
// shared by every test of the run and dropped at its end. Without the variable every test fails, because a provider is
// never tested without its database.
public sealed class MySqlDatabase : ConformanceDatabase, IAsyncLifetime
{
    private const string VariableName = "MOLTARO_WORK_MYSQL";

    private readonly string _name = "moltaro_work_test_" + Guid.NewGuid().ToString("N");

    // The connection string of the server, to create and drop the database; null until the run has created it.
    private string? _serverConnectionString;

    private MySqlDataSource? _dataSource;

    private MySqlDataSource DataSource =>
        _dataSource ?? throw new InvalidOperationException("The database of the run has not been created.");

    public async ValueTask InitializeAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string? serverConnectionString = Environment.GetEnvironmentVariable(VariableName);

        if (string.IsNullOrEmpty(serverConnectionString))
        {
            throw new InvalidOperationException(
                $"Set {VariableName} to the connection string of a MySQL 8.4 or later on which the tests may create a database.");
        }

        await using (var server = new MySqlConnection(serverConnectionString))
        {
            await server.OpenAsync(cancellationToken);

            await using var create = new MySqlCommand($"CREATE DATABASE `{_name}`", server);
            await create.ExecuteNonQueryAsync(cancellationToken);
        }

        _serverConnectionString = serverConnectionString;

        var builder = new MySqlConnectionStringBuilder(serverConnectionString) { Database = _name };
        _dataSource = new MySqlDataSource(builder.ConnectionString);

        // MySQL commits every CREATE TABLE at once, so a transaction would not keep a failure from leaving half of the
        // schema behind; the database is dropped at the end of the run either way.
        await using MySqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var apply = new MySqlCommand(MySqlWorkStore.SchemaScript, connection);
        await apply.ExecuteNonQueryAsync(cancellationToken);
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

        await using var server = new MySqlConnection(_serverConnectionString);
        await server.OpenAsync();

        await using var drop = new MySqlCommand($"DROP DATABASE `{_name}`", server);
        await drop.ExecuteNonQueryAsync();
    }

    public override IWorkStore CreateStore()
    {
        return new MySqlWorkStore(DataSource);
    }

    public override async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        return await DataSource.OpenConnectionAsync(cancellationToken);
    }

    // The one statement of this project that moves the database's clock: every time of the queue's items, NULL
    // staying NULL, is shifted back.
    public override async Task MoveClockAsync(WorkQueue queue, TimeSpan by, CancellationToken cancellationToken)
    {
        await using MySqlConnection connection = await DataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new MySqlCommand(
            """
            UPDATE moltaro_work_item
            SET due_at = due_at - INTERVAL @microseconds MICROSECOND,
                again_due_at = again_due_at - INTERVAL @microseconds MICROSECOND,
                lease_until = lease_until - INTERVAL @microseconds MICROSECOND,
                enqueued_at = enqueued_at - INTERVAL @microseconds MICROSECOND,
                claimed_at = claimed_at - INTERVAL @microseconds MICROSECOND,
                finished_at = finished_at - INTERVAL @microseconds MICROSECOND
            WHERE queue = @queue
            """,
            connection);
        command.Parameters.AddWithValue("@microseconds", by.Ticks / TimeSpan.TicksPerMicrosecond);
        command.Parameters.AddWithValue("@queue", queue.Name);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
