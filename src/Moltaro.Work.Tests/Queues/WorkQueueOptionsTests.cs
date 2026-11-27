namespace Moltaro.Work.Tests.Queues;

// The options of a queue: each at and beyond each end of its range, the defaults, and the copy the constructor keeps.
// A TimeSpan of a queue is greater than zero and at most 3650 days.
public sealed class WorkQueueOptionsTests
{
    private static readonly TimeSpan Longest = TimeSpan.FromDays(3650);

    private static readonly TimeSpan Tick = TimeSpan.FromTicks(1);

    public static TheoryData<TimeSpan> TimesOutOfRange =>
    [
        TimeSpan.Zero,
        -Tick,
        Longest + Tick,
    ];

    public static TheoryData<TimeSpan> TimesInRange =>
    [
        Tick,
        Longest,
    ];

    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public void An_attempt_limit_outside_1_to_1000_is_refused(int attemptLimit)
    {
        var options = new WorkQueueOptions { AttemptLimit = attemptLimit };

        Assert.Throws<ArgumentOutOfRangeException>(
            nameof(WorkQueueOptions.AttemptLimit),
            () => new WorkQueue("mail", WorkShapeEnum.Log, options));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1000)]
    public void An_attempt_limit_of_1_to_1000_is_kept(int attemptLimit)
    {
        var options = new WorkQueueOptions { AttemptLimit = attemptLimit };

        var queue = new WorkQueue("mail", WorkShapeEnum.Log, options);

        Assert.Equal(attemptLimit, queue.AttemptLimit);
    }

    [Theory]
    [MemberData(nameof(TimesOutOfRange))]
    public void A_retry_delay_out_of_range_is_refused(TimeSpan retryDelay)
    {
        var options = new WorkQueueOptions { RetryDelay = retryDelay, MaxRetryDelay = Longest };

        Assert.Throws<ArgumentOutOfRangeException>(
            nameof(WorkQueueOptions.RetryDelay),
            () => new WorkQueue("mail", WorkShapeEnum.Log, options));
    }

    [Theory]
    [MemberData(nameof(TimesInRange))]
    public void A_retry_delay_in_range_is_kept(TimeSpan retryDelay)
    {
        var options = new WorkQueueOptions { RetryDelay = retryDelay, MaxRetryDelay = Longest };

        var queue = new WorkQueue("mail", WorkShapeEnum.Log, options);

        Assert.Equal(retryDelay, queue.RetryDelay);
    }

    [Theory]
    [MemberData(nameof(TimesOutOfRange))]
    public void A_longest_retry_delay_out_of_range_is_refused(TimeSpan maxRetryDelay)
    {
        var options = new WorkQueueOptions { RetryDelay = Tick, MaxRetryDelay = maxRetryDelay };

        Assert.Throws<ArgumentOutOfRangeException>(
            nameof(WorkQueueOptions.MaxRetryDelay),
            () => new WorkQueue("mail", WorkShapeEnum.Log, options));
    }

    [Theory]
    [MemberData(nameof(TimesInRange))]
    public void A_longest_retry_delay_in_range_is_kept(TimeSpan maxRetryDelay)
    {
        var options = new WorkQueueOptions { RetryDelay = Tick, MaxRetryDelay = maxRetryDelay };

        var queue = new WorkQueue("mail", WorkShapeEnum.Log, options);

        Assert.Equal(maxRetryDelay, queue.MaxRetryDelay);
    }

    [Fact]
    public void A_longest_retry_delay_below_the_retry_delay_is_refused()
    {
        var options = new WorkQueueOptions
        {
            RetryDelay = TimeSpan.FromSeconds(2),
            MaxRetryDelay = TimeSpan.FromSeconds(2) - Tick,
        };

        Assert.Throws<ArgumentException>(
            nameof(WorkQueueOptions.MaxRetryDelay),
            () => new WorkQueue("mail", WorkShapeEnum.Log, options));
    }

    [Fact]
    public void A_longest_retry_delay_equal_to_the_retry_delay_is_kept()
    {
        var options = new WorkQueueOptions { RetryDelay = TimeSpan.FromSeconds(2), MaxRetryDelay = TimeSpan.FromSeconds(2) };

        var queue = new WorkQueue("mail", WorkShapeEnum.Log, options);

        Assert.Equal(queue.RetryDelay, queue.MaxRetryDelay);
    }

    [Theory]
    [MemberData(nameof(TimesOutOfRange))]
    public void A_retention_out_of_range_is_refused(TimeSpan retention)
    {
        var options = new WorkQueueOptions { Retention = retention };

        Assert.Throws<ArgumentOutOfRangeException>(
            nameof(WorkQueueOptions.Retention),
            () => new WorkQueue("mail", WorkShapeEnum.Log, options));
    }

    [Theory]
    [MemberData(nameof(TimesInRange))]
    public void A_retention_in_range_is_kept(TimeSpan retention)
    {
        var options = new WorkQueueOptions { Retention = retention };

        var queue = new WorkQueue("mail", WorkShapeEnum.Log, options);

        Assert.Equal(retention, queue.Retention);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Scoped_is_kept(bool scoped)
    {
        var queue = new WorkQueue("mail", WorkShapeEnum.Log, new WorkQueueOptions { Scoped = scoped });

        Assert.Equal(scoped, queue.Scoped);
    }

    [Fact]
    public void Null_options_give_the_defaults()
    {
        var queue = new WorkQueue("mail", WorkShapeEnum.Log, options: null);

        Assert.Equal(10, queue.AttemptLimit);
        Assert.Equal(TimeSpan.FromSeconds(10), queue.RetryDelay);
        Assert.Equal(TimeSpan.FromHours(1), queue.MaxRetryDelay);
        Assert.Equal(TimeSpan.FromDays(7), queue.Retention);
        Assert.False(queue.Scoped);
    }

    [Fact]
    public void Changing_the_options_after_the_constructor_changes_nothing()
    {
        var options = new WorkQueueOptions
        {
            AttemptLimit = 3,
            RetryDelay = TimeSpan.FromSeconds(1),
            MaxRetryDelay = TimeSpan.FromMinutes(1),
            Retention = TimeSpan.FromDays(1),
            Scoped = true,
        };
        var queue = new WorkQueue("mail", WorkShapeEnum.Log, options);

        options.AttemptLimit = 5;
        options.RetryDelay = TimeSpan.FromSeconds(2);
        options.MaxRetryDelay = TimeSpan.FromMinutes(2);
        options.Retention = TimeSpan.FromDays(2);
        options.Scoped = false;

        Assert.Equal(3, queue.AttemptLimit);
        Assert.Equal(TimeSpan.FromSeconds(1), queue.RetryDelay);
        Assert.Equal(TimeSpan.FromMinutes(1), queue.MaxRetryDelay);
        Assert.Equal(TimeSpan.FromDays(1), queue.Retention);
        Assert.True(queue.Scoped);
    }
}
