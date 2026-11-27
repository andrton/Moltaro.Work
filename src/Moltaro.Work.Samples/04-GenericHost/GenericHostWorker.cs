using System.Net;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moltaro.Signals.Hosting;
using Moltaro.Work.Hosting;
using Moltaro.Work.PostgreSql;
using Npgsql;

namespace Moltaro.Work.Samples.GenericHost;

// Receiving work in a .NET generic host: the signal host of the process, the store and the client, and a worker with
// its handler are registered in a few lines. After the start the application enqueues an item, and its handler stops
// the application, which stops the worker with it.
internal static class GenericHostWorker
{
    public static async Task RunAsync(string connectionString, CancellationToken cancellationToken)
    {
        // One item per price list, however often its prices change; the handler reads the list's current prices.
        var priceLists = new WorkQueue("price-lists", WorkShapeEnum.Coalescing);

        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        // Keeps the output of the sample to its own lines and to the log lines of the libraries.
        builder.Services.Configure<ConsoleLifetimeOptions>(options => options.SuppressStatusMessages = true);

        try
        {
            // The signal host first, so that its port is bound before the worker starts.
            builder.Services.AddSignalHost(options =>
            {
                options.ListenEndPoint = new IPEndPoint(IPAddress.Loopback, 0);
                options.Token = RandomNumberGenerator.GetHexString(32);
            });

            // The application's data source, which the container creates and disposes, and the store over it. The
            // schema is in place: 02 applied it where it was missing.
            builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
            builder.Services.AddWork(services => new PostgreSqlWorkStore(services.GetRequiredService<NpgsqlDataSource>()));

            // One worker of the queue, with two handlers at once; each item gets a scope and a handler of its own.
            builder.Services.AddWorkWorker<PriceListHandler>(priceLists, options => options.Lanes = 2);

            using IHost app = builder.Build();
            await app.StartAsync(cancellationToken);

            Console.WriteLine("The application started, and its worker of price-lists with it.");

            // Code of the application after the start, such as an API endpoint that changed the prices of list 3.
            WorkClient client = app.Services.GetRequiredService<WorkClient>();
            NpgsqlDataSource dataSource = app.Services.GetRequiredService<NpgsqlDataSource>();

            await using (NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken))
            await using (NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken))
            {
                WorkTransaction work = client.Join(transaction);
                WorkEnqueueResult enqueued = await work.EnqueueAsync(
                    priceLists,
                    key: "price-list-3",
                    cancellationToken: cancellationToken);
                await work.CommitAsync(cancellationToken);

                Console.WriteLine($"Committed price list 3: {enqueued.Status}, item {enqueued.ItemId}.");
            }

            // The handler stops the application once it has handled the item.
            await app.WaitForShutdownAsync(cancellationToken);

            Console.WriteLine("The application stopped, and its worker with it.");
        }
        catch (NotSupportedException exception)
        {
            Console.WriteLine($"Stops here: {exception.Message}");
        }
    }
}
