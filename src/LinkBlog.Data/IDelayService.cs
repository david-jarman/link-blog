namespace LinkBlog.Data;

/// <summary>
/// Provides an abstraction for time delays to enable testability.
/// </summary>
public interface IDelayService
{
    /// <summary>
    /// Creates a task that completes after a specified time interval.
    /// </summary>
    /// <param name="delay">The time span to wait before completing the returned task.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the time delay.</returns>
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default);
}