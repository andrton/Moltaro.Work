using Moltaro.Work.Samples.Worker;
using Npgsql;

namespace Moltaro.Work.Samples.LogsAndRuns;

// The two shapes with a payload, in the frame of 03: a log item, deleted once handled; and a run, enqueued twice with
// one key while it is active, so that it runs once, and kept finished with its history for the queue's retention.
internal static class LogsAndRuns
{
    public static async Task RunAsync(string connectionString, CancellationToken cancellationToken)
    {
        await using NpgsqlDataSource dataSource = NpgsqlDataSource.Create(connectionString);

        // Every mail is its own item.
        var orderMails = new WorkQueue("order-mails", WorkShapeEnum.Log);

        // Every import is a run whose history is shown for a month.
        var imports = new WorkQueue(
            "nightly-imports",
            WorkShapeEnum.Run,
            new WorkQueueOptions { Retention = TimeSpan.FromDays(30) });

        var mailHandler = new OrderMailHandler();
        var importHandler = new ImportHandler();
        long mailId = 0;
        long importId = 0;

        try
        {
            await WorkerWithoutHost.RunWorkerAsync(
                dataSource,
                orderMails,
                mailHandler,
                options: null,
                SendMailAsync,
                cancellationToken);

            // There is no read API: the application reads the tables with its own SQL.
            await PrintItemAsync(dataSource, mailId, cancellationToken);

            await WorkerWithoutHost.RunWorkerAsync(
                dataSource,
                imports,
                importHandler,
                options: null,
                ImportAsync,
                cancellationToken);

            await PrintItemAsync(dataSource, importId, cancellationToken);
        }
        catch (NotSupportedException exception)
        {
            Console.WriteLine($"Stops here: {exception.Message}");
        }

        async Task SendMailAsync(WorkClient client, CancellationToken token)
        {
            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(token);
            await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(token);

            // The payload is the application's own serialization of the event, usually JSON.
            WorkTransaction work = client.Join(transaction);
            WorkEnqueueResult mail = await work.EnqueueAsync(
                orderMails,
                payload: """{"order":17,"mail":"confirmation"}""",
                cancellationToken: token);
            await work.CommitAsync(token);

            mailId = mail.ItemId;
            Console.WriteLine($"Committed the confirmation mail of order 17: {mail.Status}, item {mail.ItemId}.");

            await mailHandler.Sent.WaitAsync(token);
        }

        async Task ImportAsync(WorkClient client, CancellationToken token)
        {
            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(token);
            await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(token);

            // Two requests of the same night's import: while the first is active, the key adds no second run.
            WorkTransaction work = client.Join(transaction);
            WorkEnqueueResult first = await work.EnqueueAsync(
                imports,
                key: "import-2026-11-27",
                payload: """{"file":"prices-2026-11-27.csv"}""",
                cancellationToken: token);
            WorkEnqueueResult second = await work.EnqueueAsync(
                imports,
                key: "import-2026-11-27",
                payload: """{"file":"prices-2026-11-27.csv"}""",
                cancellationToken: token);
            await work.CommitAsync(token);

            importId = first.ItemId;
            Console.WriteLine($"Enqueued the import twice: {first.Status}, item {first.ItemId}; then {second.Status}, item {second.ItemId}.");

            await importHandler.Imported.WaitAsync(token);
        }
    }

    // A log item is deleted after success; a run is kept finished, state 3, with its times.
    private static async Task PrintItemAsync(NpgsqlDataSource dataSource, long itemId, CancellationToken cancellationToken)
    {
        await using NpgsqlCommand read = dataSource.CreateCommand(
            "SELECT state, attempts, enqueued_at, finished_at FROM moltaro_work_item WHERE id = @id");
        read.Parameters.AddWithValue("id", itemId);

        await using NpgsqlDataReader reader = await read.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            Console.WriteLine($"Item {itemId} is deleted, as a log item is after success.");

            return;
        }

        short state = reader.GetInt16(0);
        int attempts = reader.GetInt32(1);
        DateTime enqueuedAt = reader.GetDateTime(2);
        DateTime finishedAt = reader.GetDateTime(3);

        Console.WriteLine(
            $"Item {itemId} is kept: state {state}, {attempts} attempt(s), enqueued {enqueuedAt:HH:mm:ss.fff}, "
            + $"finished {finishedAt:HH:mm:ss.fff} UTC.");
    }
}
