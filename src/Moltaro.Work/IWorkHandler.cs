namespace Moltaro.Work;

/// <summary>
/// The application's code for the items of one queue, run by a <see cref="WorkWorker"/> for each item it claims.
/// </summary>
/// <remarks>
/// <para>
/// A handler may run more than once for an item, for example when the lease of its attempt was lost after its effect.
/// It is written to be idempotent: running it again for the same item does no harm. The library never lets two
/// holders complete the same item, but it does not make the handler's effects exactly-once.
/// </para>
/// <para>
/// The handler reports the outcome of its attempt: <see cref="WorkHandlerResultEnum.Completed"/>, or
/// <see cref="WorkHandlerResultEnum.Retry"/> to ask for another attempt. An exception it throws is a failure, and so
/// is a returned value that is not defined. The reason kept with a failed attempt is the exception's
/// <see cref="Exception.ToString"/>, cut to at most 4000 characters, never inside a surrogate pair. The worker writes
/// the item's outcome after the handler returned; the handler itself writes nothing of the item.
/// </para>
/// <para>
/// The worker may run several handlers at once, one per lane, so one handler object may be called from several
/// threads at once.
/// </para>
/// </remarks>
public interface IWorkHandler
{
    /// <summary>
    /// Runs one attempt of an item.
    /// </summary>
    /// <param name="item">The item, as it was claimed for this attempt.</param>
    /// <param name="cancellationToken">
    /// Cancelled when the worker stops, or when the lease of this attempt is lost. After a stop began, an attempt that
    /// ends with anything but <see cref="WorkHandlerResultEnum.Completed"/> is released: the item is waiting again at
    /// once, with the attempts it had before this claim. After the lease was lost, the worker's write of the outcome is
    /// refused, because the item may have a new holder.
    /// </param>
    /// <returns>The outcome of the attempt.</returns>
    Task<WorkHandlerResultEnum> HandleAsync(WorkItem item, CancellationToken cancellationToken);
}
