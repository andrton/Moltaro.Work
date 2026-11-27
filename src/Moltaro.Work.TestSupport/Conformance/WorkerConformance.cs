namespace Moltaro.Work.TestSupport.Conformance;

// The worker on a real database (rules 2 and 3 of concept section 3): a commit wakes it, and it waits without polling.
// S7 writes its cases, with the signal host, the clock and the count of statements that S7's brief gives the frame.
public abstract class WorkerConformance
{
    private readonly ConformanceDatabase _database;

    protected WorkerConformance(ConformanceDatabase database)
    {
        _database = database;
    }
}
