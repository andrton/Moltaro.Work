namespace Moltaro.Work;

/// <summary>
/// What a handler reports about the attempt it ran. An exception of the handler is a failure, and a value that is not
/// defined here counts as one.
/// </summary>
public enum WorkHandlerResultEnum
{
    /// <summary>
    /// The work of the item is done. The worker completes the item, as the queue's <see cref="WorkShapeEnum"/> says
    /// for a success.
    /// </summary>
    Completed = 1,

    /// <summary>
    /// The handler asks for another attempt. As a failure does, the attempt counts: the item is due again after the
    /// queue's backoff, or dead once the queue's attempt limit is reached. The reason kept with the item is a fixed
    /// sentence saying that the handler asked for a retry.
    /// </summary>
    Retry = 2,
}
