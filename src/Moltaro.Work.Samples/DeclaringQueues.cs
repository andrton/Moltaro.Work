namespace Moltaro.Work.Samples;

// Declares one queue of each shape and a handler, as a feature of an application does once at its start. A queue is a
// declaration: nothing here touches a database.
internal static class DeclaringQueues
{
    public static void Run()
    {
        // One item per invoice, however often its lines change; the handler reads the invoice itself.
        var invoiceTotals = new WorkQueue("invoice-totals", WorkShapeEnum.Coalescing);

        // One item per mail to send, with the mail as its payload; deleted once sent.
        var mailOut = new WorkQueue(
            "mail-out",
            WorkShapeEnum.Log,
            new WorkQueueOptions { AttemptLimit = 5, RetryDelay = TimeSpan.FromSeconds(30) });

        // One run per import, kept for a month so that its history can be shown; one import per customer at a time.
        var imports = new WorkQueue(
            "imports",
            WorkShapeEnum.Run,
            new WorkQueueOptions { Retention = TimeSpan.FromDays(30), Scoped = true });

        IWorkHandler handler = new InvoiceTotalsHandler();

        Print(invoiceTotals);
        Print(mailOut);
        Print(imports);
        Console.WriteLine($"The handler of {invoiceTotals.Name} is {handler.GetType().Name}.");

        try
        {
            _ = new WorkQueue("mail.out", WorkShapeEnum.Log);
        }
        catch (ArgumentException exception)
        {
            Console.WriteLine($"A wrong name is refused: {exception.Message}");
        }

        Console.WriteLine("Stops here: enqueueing needs the PostgreSQL provider, which is not implemented yet.");
    }

    private static void Print(WorkQueue queue)
    {
        Console.WriteLine(
            $"{queue.Name}: {queue.Shape}, at most {queue.AttemptLimit} attempts, retry after {queue.RetryDelay} "
            + $"up to {queue.MaxRetryDelay}, kept for {queue.Retention}, scoped: {queue.Scoped}.");
    }
}
