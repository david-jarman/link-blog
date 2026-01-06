namespace LinkBlog.Data;

/// <summary>
/// Default implementation of <see cref="IDelayService"/> that wraps Task.Delay.
/// </summary>
public sealed class DelayService : IDelayService
{
    /// <inheritdoc/>
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default)
    {
        return Task.Delay(delay, cancellationToken);
    }
}
