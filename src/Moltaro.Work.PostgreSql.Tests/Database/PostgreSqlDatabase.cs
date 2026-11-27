using System.Data.Common;
using Moltaro.Work.PostgreSql.Tests.Database;
using Moltaro.Work.TestSupport.Conformance;
using Npgsql;

[assembly: AssemblyFixture(typeof(PostgreSqlDatabase))]

namespace Moltaro.Work.PostgreSql.Tests.Database;

// The database of one test run: created empty on the server that MOLTARO_WORK_POSTGRESQL names, given the schema
// script, shared by every test of the run and dropped at its end. Without the variable every test fails, because a
// provider is never tested without its database.
public sealed class PostgreSqlDatabase : ConformanceDatabase, IAsyncLifetime
{
    private const string VariableName = "MOLTARO_WORK_POSTGRESQL";

    private readonly string _name = "moltaro_work_test_" + Guid.NewGuid().ToString("N");

    // The connection string of the server, to create and drop the database; null until the run has created it.
    private string? _serverConnectionString;

    private NpgsqlDataSource? _dataSource;

    private NpgsqlDataSource DataSource =>
        _dataSource ?? throw new InvalidOperationException("The database of the run has not been created.");

    public async ValueTask InitializeAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string? serverConnectionString = Environment.GetEnvironmentVariable(VariableName);

        if (string.IsNullOrEmpty(serverConnectionString))
        {
            throw new InvalidOperationException(
                $"Set {VariableName} to the connection string of a PostgreSQL 15 or later on which the tests may create a database.");
        }

        await using (var server = new NpgsqlConnection(serverConnectionString))
        {
            await server.OpenAsync(cancellationToken);

            await using var create = new NpgsqlCommand(
                $"CREATE DATABASE \"{_name}\" TEMPLATE template0 ENCODING 'UTF8'",
                server);
            await create.ExecuteNonQueryAsync(cancellationToken);
        }

        _serverConnectionString = serverConnectionString;

        var builder = new NpgsqlConnectionStringBuilder(serverConnectionString) { Database = _name };
        _dataSource = NpgsqlDataSource.Create(builder);

        // The script runs in one transaction, so that a failure leaves no half of the schema behind.
        await using NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var apply = new NpgsqlCommand(PostgreSqlWorkStore.SchemaScript, connection, transaction);
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

        await using var server = new NpgsqlConnection(_serverConnectionString);
        await server.OpenAsync();

        await using var drop = new NpgsqlCommand($"DROP DATABASE \"{_name}\" WITH (FORCE)", server);
        await drop.ExecuteNonQueryAsync();
    }

    public override IWorkStore CreateStore()
    {
        return new PostgreSqlWorkStore(DataSource);
    }

    public override async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        return await DataSource.OpenConnectionAsync(cancellationToken);
    }

    // The one statement of this project that moves the database's clock: every time of the queue's items, NULL
    // staying NULL, is shifted back.
    public override async Task MoveClockAsync(WorkQueue queue, TimeSpan by, CancellationToken cancellationToken)
    {
        await using NpgsqlCommand command = DataSource.CreateCommand(
            """
            UPDATE moltaro_work_item
            SET due_at = due_at - @by,
                again_due_at = again_due_at - @by,
                lease_until = lease_until - @by,
                enqueued_at = enqueued_at - @by,
                claimed_at = claimed_at - @by,
                finished_at = finished_at - @by
            WHERE queue = @queue
            """);
        command.Parameters.AddWithValue("by", by);
        command.Parameters.AddWithValue("queue", queue.Name);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
