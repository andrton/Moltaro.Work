namespace Moltaro.Work;

/// <summary>
/// The data of <see cref="WorkWorker.Failed"/>: a statement of the worker that failed.
/// </summary>
public sealed class WorkWorkerFailedEventArgs : EventArgs
{
    internal WorkWorkerFailedEventArgs(Exception exception)
    {
        Exception = exception;
    }

    /// <summary>
    /// The exception of the statement that failed, such as the driver's <see cref="System.Data.Common.DbException"/>
    /// when the database is unreachable.
    /// </summary>
    public Exception Exception { get; }
}
