using Moltaro.Work.Samples.Worker;
using Npgsql;

namespace Moltaro.Work.Samples.Scopes;

// Scopes, in the frame of 03: the items of one customer are handled one after another, while those of another customer
// run beside them, on a worker with two lanes.
internal static class ScopedItems
{
    public static async Task RunAsync(string connectionString, CancellationToken cancellationToken)
    {
        await using NpgsqlDataSource dataSource = NpgsqlDataSource.Create(connectionString);

        // One item per change of a customer, synchronized to another system; the scope is the customer.
        var customerSyncs = new WorkQueue("customer-syncs", WorkShapeEnum.Log, new WorkQueueOptions { Scoped = true });
        var handler = new CustomerSyncHandler(itemCount: 3);

        try
        {
            await WorkerWithoutHost.RunWorkerAsync(
                dataSource,
                customerSyncs,
                handler,
                new WorkWorkerOptions { Lanes = 2 },
                SyncAsync,
                cancellationToken);
        }
        catch (NotSupportedException exception)
        {
            Console.WriteLine($"Stops here: {exception.Message}");
        }

        async Task SyncAsync(WorkClient client, CancellationToken token)
        {
            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(token);
            await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(token);

            // Two changes of customer 1, in order, and one of customer 2.
            WorkTransaction work = client.Join(transaction);
            await work.EnqueueAsync(customerSyncs, scope: "customer-1", payload: """{"name":"Ada"}""", cancellationToken: token);
            await work.EnqueueAsync(customerSyncs, scope: "customer-1", payload: """{"name":"Ada L."}""", cancellationToken: token);
            await work.EnqueueAsync(customerSyncs, scope: "customer-2", payload: """{"name":"Grace"}""", cancellationToken: token);
            await work.CommitAsync(token);

            Console.WriteLine("Committed two changes of customer-1 and one of customer-2.");

            await handler.AllHandled.WaitAsync(token);
        }
    }
}
