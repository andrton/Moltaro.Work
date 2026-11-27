using Moltaro.Work.Samples.Enqueue;
using Moltaro.Work.Samples.Start;

// "dotnet run" runs every sample of intended use once, in the order of their numbered folders. A sample that reaches
// what is not implemented yet says where it stops, and the next one runs. From 02 on the samples use PostgreSQL.
Console.WriteLine("== 01 Declaring queues and a handler");
DeclaringQueues.Run();

string? connectionString = Environment.GetEnvironmentVariable("MOLTARO_WORK_POSTGRESQL");

if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine();
    Console.WriteLine(
        "The samples from 02 on use PostgreSQL 15 or later: set MOLTARO_WORK_POSTGRESQL to the connection string of a "
        + "database where they may create their tables. Only 01 ran, which needs no database.");

    return;
}

Console.WriteLine();
Console.WriteLine("== 02 Enqueueing in a transaction");
await EnqueueInTransaction.RunAsync(connectionString, CancellationToken.None);
