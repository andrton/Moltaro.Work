using System.Data.Common;
using Npgsql;

namespace Moltaro.Work.PostgreSql;

/// <summary>
/// The store of Moltaro.Work on PostgreSQL: the provider of an application whose database is PostgreSQL.
/// </summary>
/// <remarks>
/// <para>
/// It works with PostgreSQL 15 or later, the oldest release supported on 26 November 2026, and with a database whose
/// encoding is UTF8. <see cref="CheckDatabaseAsync"/> checks the encoding together with the schema version.
/// </para>
/// <para>
/// Its tables are those of <see cref="SchemaScript"/>, of version <see cref="SchemaVersion"/>, in the connection's
/// default schema. The application applies the script with its own migration tool before a worker runs.
/// </para>
/// <para>
/// The transactions of its own operations run at READ COMMITTED, which it sets on each. The caller's transaction of
/// <see cref="EnqueueAsync"/> may run at any isolation. At REPEATABLE READ or SERIALIZABLE an enqueue that meets a
/// concurrent change of its key fails with <c>serialization_failure</c> (40001), and at any isolation an enqueue may
/// fail with <c>deadlock_detected</c> (40P01), both as a <see cref="PostgresException"/>: the caller rolls its
/// transaction back and retries it, as for any write.
/// </para>
/// <para>
/// Every member may be called from any thread at any time. Not implemented yet: every operation of
/// <see cref="IWorkStore"/> throws <see cref="NotSupportedException"/> after its checks, and a token that is already
/// cancelled gives a cancelled task before that.
/// </para>
/// </remarks>
public sealed class PostgreSqlWorkStore : IWorkStore
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
    /// Creates a store over the data source of the application's PostgreSQL database.
    /// </summary>
    /// <param name="dataSource">
    /// The data source of the database, usually an <see cref="NpgsqlDataSource"/>, whose connections are
    /// <see cref="NpgsqlConnection"/>. The store keeps it and opens its own connections from it; the application owns
    /// it and disposes it after the store's last use.
    /// </param>
    /// <remarks>
    /// To see the type of the data source's connections it creates one connection, opens nothing and disposes it.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="dataSource"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// The connections of <paramref name="dataSource"/> are not <see cref="NpgsqlConnection"/>.
    /// </exception>
    public PostgreSqlWorkStore(DbDataSource dataSource)
    {
        ArgumentNullException.ThrowIfNull(dataSource);

        using (DbConnection connection = dataSource.CreateConnection())
        {
            if (connection is not NpgsqlConnection)
            {
                throw new ArgumentException(
                    "The data source creates connections of another database: the PostgreSQL store needs connections of Npgsql.",
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
    /// The application applies it once, with its own migration tool, before a worker runs. It is the file
    /// <c>Schema/version-1.sql</c> of the package.
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
        if (transaction.Connection is not NpgsqlConnection)
        {
            throw new ArgumentException(
                "The transaction's connection is not one of Npgsql: the PostgreSQL store enqueues only on PostgreSQL.",
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

        throw new NotSupportedException($"{operation} on PostgreSQL is not implemented yet.");
    }

    private static string ReadSchemaScript()
    {
        using Stream stream = typeof(PostgreSqlWorkStore).Assembly
            .GetManifestResourceStream("Moltaro.Work.PostgreSql.Schema.version-1.sql")!;
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
