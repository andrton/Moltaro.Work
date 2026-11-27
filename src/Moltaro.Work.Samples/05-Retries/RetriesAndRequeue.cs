using Moltaro.Work.Samples.Worker;
using Npgsql;

namespace Moltaro.Work.Samples.Retries;

// Failures, in the frame of 03: a handler that asks for a retry and completes at its next attempt; a handler that
// throws until its item is dead after the queue's attempt limit, kept with its reason; and the requeue that puts the
// dead item back once the cause is fixed.
internal static class RetriesAndRequeue
{
    public static async Task RunAsync(string connectionString, CancellationToken cancellationToken)
    {
        await using NpgsqlDataSource dataSource = NpgsqlDataSource.Create(connectionString);

        // One item per export; a retry waits 200 milliseconds, then twice as long after each failure, up to a second.
        var exports = new WorkQueue(
            "invoice-exports",
            WorkShapeEnum.Log,
            new WorkQueueOptions { RetryDelay = TimeSpan.FromMilliseconds(200), MaxRetryDelay = TimeSpan.FromSeconds(1) });

        // One item per delivery, dead after three failed attempts.
        var webhooks = new WorkQueue(
            "webhook-deliveries",
            WorkShapeEnum.Log,
            new WorkQueueOptions
            {
                AttemptLimit = 3,
                RetryDelay = TimeSpan.FromMilliseconds(200),
                MaxRetryDelay = TimeSpan.FromSeconds(1),
            });

        var exportHandler = new InvoiceExportHandler();
        var webhookHandler = new WebhookHandler();
        long deliveryId = 0;

        try
        {
            await WorkerWithoutHost.RunWorkerAsync(
                dataSource,
                exports,
                exportHandler,
                options: null,
                ExportAsync,
                cancellationToken);

            await WorkerWithoutHost.RunWorkerAsync(
                dataSource,
                webhooks,
                webhookHandler,
                options: null,
                DeliverAsync,
                cancellationToken);

            // There is no read API: the tables are part of the contract, and the application reads them with its own SQL.
            await PrintItemAsync(dataSource, deliveryId, cancellationToken);

            // The receiver is fixed; the dead item is put back and delivered.
            webhookHandler.ReceiverIsUp = true;
            await WorkerWithoutHost.RunWorkerAsync(
                dataSource,
                webhooks,
                webhookHandler,
                options: null,
                RequeueAsync,
                cancellationToken);
        }
        catch (NotSupportedException exception)
        {
            Console.WriteLine($"Stops here: {exception.Message}");
        }

        async Task ExportAsync(WorkClient client, CancellationToken token)
        {
            await EnqueueAsync(dataSource, client, exports, """{"invoice":42}""", token);

            WorkItem exported = await exportHandler.Completed.WaitAsync(token);

            Console.WriteLine($"Item {exported.Id} completed at attempt {exported.Attempt}, after its backoff.");
        }

        async Task DeliverAsync(WorkClient client, CancellationToken token)
        {
            deliveryId = await EnqueueAsync(dataSource, client, webhooks, """{"order":18,"event":"paid"}""", token);

            // The worker writes the outcome of that last attempt before its stop completes.
            await webhookHandler.LastAttemptFailed.WaitAsync(token);
        }

        async Task RequeueAsync(WorkClient client, CancellationToken token)
        {
            WorkRequeueStatusEnum requeued = await client.RequeueAsync(webhooks, deliveryId, token);

            Console.WriteLine($"Requeued item {deliveryId}: {requeued}.");

            WorkItem delivered = await webhookHandler.Delivered.WaitAsync(token);

            Console.WriteLine($"Item {delivered.Id} was delivered at attempt {delivered.Attempt}, counted from zero again.");
        }
    }

    // A transaction of the application that enqueues one log item with its payload; returns the item's id.
    private static async Task<long> EnqueueAsync(
        NpgsqlDataSource dataSource,
        WorkClient client,
        WorkQueue queue,
        string payload,
        CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        WorkTransaction work = client.Join(transaction);
        WorkEnqueueResult enqueued = await work.EnqueueAsync(queue, payload: payload, cancellationToken: cancellationToken);
        await work.CommitAsync(cancellationToken);

        Console.WriteLine($"Committed item {enqueued.ItemId} of {queue.Name}: {payload}");

        return enqueued.ItemId;
    }

    private static async Task PrintItemAsync(NpgsqlDataSource dataSource, long itemId, CancellationToken cancellationToken)
    {
        await using NpgsqlCommand read = dataSource.CreateCommand(
            "SELECT state, attempts, reason FROM moltaro_work_item WHERE id = @id");
        read.Parameters.AddWithValue("id", itemId);

        await using NpgsqlDataReader reader = await read.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);

        // State 4 is dead. The reason is the last failure: the exception's text, whose first line is printed here.
        short state = reader.GetInt16(0);
        int attempts = reader.GetInt32(1);
        string reason = reader.GetString(2).ReplaceLineEndings("\n").Split('\n')[0];

        Console.WriteLine($"Item {itemId}: state {state}, {attempts} attempts, reason \"{reason}\".");
    }
}
