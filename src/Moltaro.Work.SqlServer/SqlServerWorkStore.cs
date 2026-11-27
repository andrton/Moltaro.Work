using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace Moltaro.Work.SqlServer;

/// <summary>
/// The store of Moltaro.Work on SQL Server: the provider of an application whose database is SQL Server or Azure SQL
/// Database.
/// </summary>
/// <remarks>
/// <para>
/// It works with SQL Server 2019 or later, or with Azure SQL Database. The database needs READ_COMMITTED_SNAPSHOT ON,
/// because without it the reads of the candidates of a claim deadlock with claims; <see cref="CheckDatabaseAsync"/>
/// checks it together with the schema version, before a worker runs. Its claims read with <c>UPDLOCK, READPAST</c>,
/// because under that setting a plain read does not skip locked rows.
/// </para>
/// <para>
/// Its tables are those of <see cref="SchemaScript"/>, of version <see cref="SchemaVersion"/>, in the connection's
/// default schema. The application applies the script with its own migration tool before a worker runs. Lock
/// escalation is off for the item table, so that a large claim or delete never locks every queue.
/// </para>
/// <para>
/// Every session that changes the tables, the caller's included, needs the SET options of filtered indexes:
/// ANSI_NULLS, ANSI_PADDING, ANSI_WARNINGS, ARITHABORT, CONCAT_NULL_YIELDS_NULL and QUOTED_IDENTIFIER ON, and
/// NUMERIC_ROUNDABORT OFF. The defaults of Microsoft.Data.SqlClient meet them at a compatibility level of 90 or higher.
/// The script sets them itself, because <c>sqlcmd</c> leaves QUOTED_IDENTIFIER OFF.
/// </para>
/// <para>
/// The transactions of its own operations run at READ COMMITTED. The caller's transaction of
/// <see cref="EnqueueAsync"/> may run at any isolation. At any isolation an enqueue may fail as a deadlock victim
/// (1205), and under SNAPSHOT with an update conflict (3960), both as a <see cref="SqlException"/>: the caller rolls
/// its transaction back and retries it, as for any write.
/// </para>
/// <para>
/// Once the token of any of its operations is cancelled, a failure of the driver ends the task cancelled, with the
/// driver's exception as its inner one. Microsoft.Data.SqlClient reports the cancellation of a command it sent as a
/// <see cref="SqlException"/> of error number 0, "Operation cancelled by user.", and of a command not yet sent as an
/// <see cref="InvalidOperationException"/>: that is the caller's cancellation, not a database error.
/// </para>
/// <para>
/// Every member may be called from any thread at any time. Not implemented yet: every operation of
/// <see cref="IWorkStore"/> throws <see cref="NotSupportedException"/> after its checks, and a token that is already
/// cancelled gives a cancelled task before that.
/// </para>
/// </remarks>
public sealed class SqlServerWorkStore : IWorkStore
{
    /// <summary>
    /// The version of the schema that this store needs and that <see cref="SchemaScript"/> creates. The one row of the
    /// table <c>moltaro_work_schema</c> holds it; a later version comes with a script that changes the tables and that
    /// row.
    /// </summary>
    public const int SchemaVersion = 1;

    // The application's data source, from which every operation but an enqueue opens its connection. The application
    // owns it and disposes it.
    private readonly DbDataSource _dataSource;

    /// <summary>
    /// Creates a store over the data source of the application's SQL Server database.
    /// </summary>
    /// <param name="dataSource">
    /// The data source of the database, usually one that <c>SqlClientFactory.Instance.CreateDataSource</c> creates,
    /// whose connections are <see cref="SqlConnection"/> of Microsoft.Data.SqlClient. The store keeps it and opens its
    /// own connections from it; the application owns it and disposes it after the store's last use.
    /// </param>
    /// <remarks>
    /// To see the type of the data source's connections it creates one connection, opens nothing and disposes it.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="dataSource"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// The connections of <paramref name="dataSource"/> are not <see cref="SqlConnection"/> of Microsoft.Data.SqlClient.
    /// </exception>
    public SqlServerWorkStore(DbDataSource dataSource)
    {
        ArgumentNullException.ThrowIfNull(dataSource);

        using (DbConnection connection = dataSource.CreateConnection())
        {
            if (connection is not SqlConnection)
            {
                throw new ArgumentException(
                    "The data source creates connections of another database: the SQL Server store needs connections of Microsoft.Data.SqlClient.",
                    nameof(dataSource));
            }
        }

        _dataSource = dataSource;
    }

    /// <summary>
    /// The schema script of version <see cref="SchemaVersion"/>: the text that creates the store's tables in the
    /// connection's default schema.
    /// </summary>
    /// <remarks>
    /// The application applies it once, with its own migration tool, before a worker runs. It is one batch, without
    /// <c>GO</c>, and sets the SET options its filtered indexes need. The assembly carries it as an embedded resource,
    /// made from the source file <c>Schema/version-1.sql</c> of the provider.
    /// </remarks>
    public static string SchemaScript { get; } = ReadSchemaScript();

    /// <inheritdoc/>
    public Task CheckDatabaseAsync(CancellationToken cancellationToken)
    {
        return NotImplementedYet<bool>("Checking the database", cancellationToken);
    }

    /// <inheritdoc/>
    public Task<WorkEnqueueResult> EnqueueAsync(
        DbTransaction transaction,
        WorkQueue queue,
        string? key,
        string? scope,
        string? payload,
        DateTimeOffset? dueAt,
        CancellationToken cancellationToken)
    {
        if (transaction.Connection is not SqlConnection)
        {
            throw new ArgumentException(
                "The transaction's connection is not one of Microsoft.Data.SqlClient: the SQL Server store enqueues only on SQL Server.",
                nameof(transaction));
        }

        return NotImplementedYet<WorkEnqueueResult>("Enqueueing", cancellationToken);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<WorkClaim>> ClaimAsync(
        WorkQueue queue,
        int count,
        TimeSpan lease,
        CancellationToken cancellationToken)
    {
        return NotImplementedYet<IReadOnlyList<WorkClaim>>("Claiming", cancellationToken);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<WorkClaim>> HeartbeatAsync(
        IReadOnlyList<WorkClaim> claims,
        TimeSpan lease,
        CancellationToken cancellationToken)
    {
        return NotImplementedYet<IReadOnlyList<WorkClaim>>("Renewing a lease", cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> CompleteAsync(WorkClaim claim, CancellationToken cancellationToken)
    {
        return NotImplementedYet<bool>("Completing an item", cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> RetryAsync(WorkClaim claim, TimeSpan delay, string reason, CancellationToken cancellationToken)
    {
        return NotImplementedYet<bool>("Retrying an item", cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> GiveUpAsync(WorkClaim claim, string reason, CancellationToken cancellationToken)
    {
        return NotImplementedYet<bool>("Giving up an item", cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> ReleaseAsync(WorkClaim claim, CancellationToken cancellationToken)
    {
        return NotImplementedYet<bool>("Releasing an item", cancellationToken);
    }

    /// <inheritdoc/>
    public Task<WorkRequeueStatusEnum> RequeueAsync(WorkQueue queue, long itemId, CancellationToken cancellationToken)
    {
        return NotImplementedYet<WorkRequeueStatusEnum>("Requeueing an item", cancellationToken);
    }

    /// <inheritdoc/>
    public Task<WorkNextDue> GetNextDueAsync(WorkQueue queue, CancellationToken cancellationToken)
    {
        return NotImplementedYet<WorkNextDue>("Reading the next due times", cancellationToken);
    }

    /// <inheritdoc/>
    public Task<int> DeleteExpiredAsync(WorkQueue queue, int count, CancellationToken cancellationToken)
    {
        return NotImplementedYet<int>("Deleting expired items", cancellationToken);
    }

    // The refusal of an operation until the sprint that implements it: a token that is already cancelled gives a
    // cancelled task first, as for every operation of the store.
    private static Task<T> NotImplementedYet<T>(string operation, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<T>(cancellationToken);
        }

        throw new NotSupportedException($"{operation} on SQL Server is not implemented yet.");
    }

    private static string ReadSchemaScript()
    {
        using Stream stream = typeof(SqlServerWorkStore).Assembly
            .GetManifestResourceStream("Moltaro.Work.SqlServer.Schema.version-1.sql")!;
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
