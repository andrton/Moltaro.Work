namespace Moltaro.Work.TestSupport.Conformance;

// The worker on a real database (rules 2 and 3 of concept section 3): a commit wakes it, and it waits without polling.
//
// These cases are named here and written by S7. Their bodies need what this frame does not have yet: a signal host and
// a clock that the host and the worker share, and a count of the statements a worker sends, through the driver's
// tracing, which S7's brief gives the frame. Until then each fails if its skip is removed.
public abstract class WorkerConformance
{
    private const string WrittenByS7 = "This case is written with the frame of S7.";

    private readonly ConformanceDatabase _database;

    protected WorkerConformance(ConformanceDatabase database)
    {
        _database = database;
    }

    [Fact(Skip = SkipReason.NotImplemented)]
    public void A_commit_starts_the_handler_without_moving_any_clock()
    {
        Assert.Fail(WrittenByS7);
    }

    [Fact(Skip = SkipReason.NotImplemented)]
    public void An_idle_worker_sends_no_statement_before_its_idle_bound()
    {
        Assert.Fail(WrittenByS7);
    }

    [Fact(Skip = SkipReason.NotImplemented)]
    public void A_due_item_locked_by_an_open_transaction_costs_no_statement()
    {
        Assert.Fail(WrittenByS7);
    }

    [Fact(Skip = SkipReason.NotImplemented)]
    public void Two_workers_share_a_burst_of_items_without_waiting_for_an_idle_bound()
    {
        Assert.Fail(WrittenByS7);
    }
}
