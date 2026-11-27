using System.Net;
using System.Security.Cryptography;
using Moltaro.Signals;
using Moltaro.Work.PostgreSql;
using Npgsql;

namespace Moltaro.Work.Samples.Worker;

// Receiving work without a generic host: a handler and a WorkWorker over an in-process SignalHost handle what a commit
// sent, then the worker stops. RunWorkerAsync is the frame of it, which the samples 05 to 07 run in as well.
internal static class WorkerWithoutHost
{
    public static async Task RunAsync(string connectionString, CancellationToken cancellationToken)
    {
        // The schema is in place: 02 applied it where it was missing. The application owns its data source.
        await using NpgsqlDataSource dataSource = NpgsqlDataSource.Create(connectionString);

        // One item per product, however often its stock changes; the handler reads the product's current stock.
        var stockLevels = new WorkQueue("stock-levels", WorkShapeEnum.Coalescing);
        var handler = new StockLevelHandler();

        try
        {
            await RunWorkerAsync(dataSource, stockLevels, handler, options: null, SendAndWaitAsync, cancellationToken);
        }
        catch (NotSupportedException exception)
        {
            Console.WriteLine($"Stops here: {exception.Message}");
        }

        // While the worker runs: a commit sends an item, and the worker handles it.
        async Task SendAndWaitAsync(WorkClient client, CancellationToken token)
        {
            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(token);
            await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(token);

            WorkTransaction work = client.Join(transaction);
            WorkEnqueueResult enqueued = await work.EnqueueAsync(stockLevels, key: "product-7", cancellationToken: token);
            await work.CommitAsync(token);

            Console.WriteLine($"Committed the stock levels of product 7: {enqueued.Status}, item {enqueued.ItemId}.");

            WorkItem handled = await handler.Handled.WaitAsync(token);

            Console.WriteLine($"The worker handled item {handled.Id} at once, woken by the commit.");
        }
    }

    // The frame of a worker without a host. It creates what an application creates once at its start, runs one worker
    // of the queue while whileRunning does what the application does, and then stops the worker as the application
    // does when it ends: the worker cancels its running handlers, waits for them and writes their outcomes.
    public static async Task RunWorkerAsync(
        NpgsqlDataSource dataSource,
        WorkQueue queue,
        IWorkHandler handler,
        WorkWorkerOptions? options,
        Func<WorkClient, CancellationToken, Task> whileRunning,
        CancellationToken cancellationToken)
    {
        // The signals of the process: the in-process side of a host, which needs no socket, so the host is not
        // started. Commits set the queue's signal in it, and the worker waits for that signal.
        var signalOptions = new SignalHostOptions
        {
            ListenEndPoint = new IPEndPoint(IPAddress.Loopback, 0),
            Token = RandomNumberGenerator.GetHexString(32),
        };

        await using var signals = new SignalHost(signalOptions);

        // The client's clock is the system clock, as the host's is: the worker computes the deadlines of its waits on
        // the clock of the signals it waits for.
        var client = new WorkClient(new PostgreSqlWorkStore(dataSource), signals);
        var worker = new WorkWorker(client, queue, handler, options);

        // A failure of the database never stops the worker: it says so here and tries again after its failure delay.
        worker.Failed += (_, failed) =>
            Console.WriteLine($"The worker of {queue.Name} failed and tries again: {failed.Exception.Message}");

        using var stop = new CancellationTokenSource();
        Task run = worker.RunAsync(stop.Token);

        Console.WriteLine($"The worker of {queue.Name} runs.");

        try
        {
            await whileRunning(client, cancellationToken);
        }
        finally
        {
            await stop.CancelAsync();
            await run;
        }

        Console.WriteLine($"The worker of {queue.Name} stopped.");
    }
}
