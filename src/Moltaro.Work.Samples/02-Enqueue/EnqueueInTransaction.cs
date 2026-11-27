using System.Net;
using System.Security.Cryptography;
using Moltaro.Signals;
using Moltaro.Work.PostgreSql;
using Npgsql;

namespace Moltaro.Work.Samples.Enqueue;

// Sending work: a business write and a coalescing enqueue in one transaction of the application, committed through the
// library. The item exists if and only if the invoice line does, and once the commit succeeded the workers of the queue
// are woken.
internal static class EnqueueInTransaction
{
    public static async Task RunAsync(string connectionString, CancellationToken cancellationToken)
    {
        // The application owns its data source and disposes it after the store's last use.
        await using NpgsqlDataSource dataSource = NpgsqlDataSource.Create(connectionString);

        await ApplySchemaWhereMissingAsync(dataSource, cancellationToken);
        await CreateInvoiceLinesWhereMissingAsync(dataSource, cancellationToken);

        // The signals through which a commit wakes the workers: here the in-process side of a host, which needs no
        // socket, so the host is not started. A process that only sends work would use a SignalClient of its host.
        var signalOptions = new SignalHostOptions
        {
            ListenEndPoint = new IPEndPoint(IPAddress.Loopback, 0),
            Token = RandomNumberGenerator.GetHexString(32),
        };

        await using var signals = new SignalHost(signalOptions);

        // One client serves the whole application; the queue is declared once, as in 01.
        var client = new WorkClient(new PostgreSqlWorkStore(dataSource), signals);
        var invoiceTotals = new WorkQueue("invoice-totals", WorkShapeEnum.Coalescing);

        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var addLine = new NpgsqlCommand(
            "INSERT INTO sample_invoice_line (invoice_id, amount) VALUES (@invoice, @amount)",
            connection,
            transaction))
        {
            addLine.Parameters.AddWithValue("invoice", 42L);
            addLine.Parameters.AddWithValue("amount", 19.90m);
            await addLine.ExecuteNonQueryAsync(cancellationToken);
        }

        Console.WriteLine("Added a line to invoice 42, in a transaction not committed yet.");

        WorkTransaction work = client.Join(transaction);

        try
        {
            // The key names the invoice: however many lines change before a worker takes it, it is one item.
            WorkEnqueueResult enqueued = await work.EnqueueAsync(
                invoiceTotals,
                key: "invoice-42",
                cancellationToken: cancellationToken);

            Console.WriteLine($"Enqueued the totals of invoice 42 in the same transaction: {enqueued.Status}, item {enqueued.ItemId}.");

            await work.CommitAsync(cancellationToken);

            Console.WriteLine("Committed: the line and its item exist together, and a worker of invoice-totals is woken.");
        }
        catch (NotSupportedException)
        {
            // The transaction is disposed without a commit, so it rolls back: the line goes with it.
            Console.WriteLine("Stops here: enqueueing on PostgreSQL is not implemented yet.");
        }
    }

    // An application applies the schema script with its own migration tool. The sample applies it where it is missing,
    // in one transaction, so that a failure leaves no half of it behind.
    private static async Task ApplySchemaWhereMissingAsync(NpgsqlDataSource dataSource, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var find = new NpgsqlCommand("SELECT to_regclass('moltaro_work_schema') IS NOT NULL", connection, transaction))
        {
            if ((bool)(await find.ExecuteScalarAsync(cancellationToken))!)
            {
                Console.WriteLine("The schema of Moltaro.Work is in place.");

                return;
            }
        }

        await using (var apply = new NpgsqlCommand(PostgreSqlWorkStore.SchemaScript, connection, transaction))
        {
            await apply.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        Console.WriteLine($"Applied the schema of Moltaro.Work, version {PostgreSqlWorkStore.SchemaVersion}.");
    }

    // The application's own table, which the business write changes.
    private static async Task CreateInvoiceLinesWhereMissingAsync(NpgsqlDataSource dataSource, CancellationToken cancellationToken)
    {
        await using NpgsqlCommand create = dataSource.CreateCommand(
            "CREATE TABLE IF NOT EXISTS sample_invoice_line (invoice_id bigint NOT NULL, amount numeric(12, 2) NOT NULL)");

        await create.ExecuteNonQueryAsync(cancellationToken);
    }
}
