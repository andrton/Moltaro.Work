namespace Moltaro.Work;

/// <summary>
/// What a queue has next, as the database computed it from its own now: the durations a worker waits for.
/// </summary>
/// <param name="UntilClaimable">
/// The time until the queue's next item may be claimed: zero when one already may, an item whose lease has ended
/// included; <see langword="null"/> when no item is waiting and no lease has ended. It is never the time until a
/// lease ends.
/// </param>
/// <param name="UntilExpired">
/// The time until the queue's next dead item or finished run outlives the retention: zero when one already has;
/// <see langword="null"/> when there is none.
/// </param>
/// <remarks>
/// Each value is a duration, not an instant, so that no two clocks are compared: the worker adds the shorter one,
/// bounded by its idle bound, to the now of its client's clock to find the deadline of its wait.
/// </remarks>
public readonly record struct WorkNextDue(TimeSpan? UntilClaimable, TimeSpan? UntilExpired);
