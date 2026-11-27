using Moltaro.Signals;
using Moltaro.Work.Tests.Doubles;

namespace Moltaro.Work.Tests.Workers;

// The options of a worker, checked by the constructor of WorkWorker: each at and beyond each end of its range, the
// heartbeat at and above half of the lease, and the defaults. A TimeSpan of a worker is greater than zero and at most
// the longest timer of TimeProvider.
public sealed class WorkWorkerOptionsTests : IAsyncDisposable
{
    private static readonly TimeSpan Longest = TimeSpan.FromMilliseconds(uint.MaxValue - 1);

    private static readonly TimeSpan Tick = TimeSpan.FromTicks(1);

    private static readonly WorkQueue Balances = new("balances", WorkShapeEnum.Coalescing);

    private readonly SignalHost _signals = LoopbackSignalHost.Create();

    private readonly WorkClient _client;

    public WorkWorkerOptionsTests()
    {
        _client = new WorkClient(new RecordingWorkStore(), _signals);
    }

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

    public static TheoryData<TimeSpan> LeasesInRange =>
    [
        TimeSpan.FromTicks(2),
        Longest,
    ];

    public ValueTask DisposeAsync()
    {
        return _signals.DisposeAsync();
    }

    [Fact]
    public void The_defaults_are_those_the_options_document()
    {
        var options = new WorkWorkerOptions();

        Assert.Equal(1, options.Lanes);
        Assert.Equal(TimeSpan.FromSeconds(30), options.LeaseDuration);
        Assert.Equal(TimeSpan.FromSeconds(10), options.HeartbeatInterval);
        Assert.Equal(TimeSpan.FromMinutes(1), options.IdleBound);
        Assert.Equal(TimeSpan.FromSeconds(5), options.FailureDelay);
        Assert.Equal(1000, options.RetentionBatchSize);
        Assert.Same(TimeProvider.System, options.TimeProvider);
    }

    [Fact]
    public void Null_options_are_accepted_as_the_defaults()
    {
        _ = new WorkWorker(_client, Balances, new CompletingHandler(), options: null);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public void Lanes_outside_1_to_1000_are_refused(int lanes)
    {
        AssertOutOfRange(nameof(WorkWorkerOptions.Lanes), new WorkWorkerOptions { Lanes = lanes });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1000)]
    public void Lanes_of_1_to_1000_are_accepted(int lanes)
    {
        CreateWorker(new WorkWorkerOptions { Lanes = lanes });
    }

    [Theory]
    [MemberData(nameof(TimesOutOfRange))]
    public void A_lease_duration_out_of_range_is_refused(TimeSpan leaseDuration)
    {
        AssertOutOfRange(
            nameof(WorkWorkerOptions.LeaseDuration),
            new WorkWorkerOptions { LeaseDuration = leaseDuration, HeartbeatInterval = Tick });
    }

    // The shortest lease that leaves room for a heartbeat is two ticks.
    [Theory]
    [MemberData(nameof(LeasesInRange))]
    public void A_lease_duration_in_range_is_accepted(TimeSpan leaseDuration)
    {
        CreateWorker(new WorkWorkerOptions { LeaseDuration = leaseDuration, HeartbeatInterval = Tick });
    }

    [Theory]
    [MemberData(nameof(TimesOutOfRange))]
    public void A_heartbeat_interval_out_of_range_is_refused(TimeSpan heartbeatInterval)
    {
        AssertOutOfRange(
            nameof(WorkWorkerOptions.HeartbeatInterval),
            new WorkWorkerOptions { LeaseDuration = Longest, HeartbeatInterval = heartbeatInterval });
    }

    [Theory]
    [InlineData(300_000_000L)]
    [InlineData(3L)]
    public void A_heartbeat_interval_of_half_the_lease_duration_is_accepted(long leaseTicks)
    {
        TimeSpan leaseDuration = TimeSpan.FromTicks(leaseTicks);

        CreateWorker(new WorkWorkerOptions
        {
            LeaseDuration = leaseDuration,
            HeartbeatInterval = TimeSpan.FromTicks(leaseDuration.Ticks / 2),
        });
    }

    [Fact]
    public void A_heartbeat_interval_of_one_tick_is_accepted()
    {
        CreateWorker(new WorkWorkerOptions { HeartbeatInterval = Tick });
    }

    [Theory]
    [InlineData(300_000_000L)]
    [InlineData(3L)]
    public void A_heartbeat_interval_above_half_the_lease_duration_is_refused(long leaseTicks)
    {
        TimeSpan leaseDuration = TimeSpan.FromTicks(leaseTicks);
        var options = new WorkWorkerOptions
        {
            LeaseDuration = leaseDuration,
            HeartbeatInterval = TimeSpan.FromTicks(leaseDuration.Ticks / 2) + Tick,
        };

        Assert.Throws<ArgumentException>(nameof(WorkWorkerOptions.HeartbeatInterval), () => CreateWorker(options));
    }

    [Theory]
    [MemberData(nameof(TimesOutOfRange))]
    public void An_idle_bound_out_of_range_is_refused(TimeSpan idleBound)
    {
        AssertOutOfRange(nameof(WorkWorkerOptions.IdleBound), new WorkWorkerOptions { IdleBound = idleBound });
    }

    [Theory]
    [MemberData(nameof(TimesInRange))]
    public void An_idle_bound_in_range_is_accepted(TimeSpan idleBound)
    {
        CreateWorker(new WorkWorkerOptions { IdleBound = idleBound });
    }

    [Theory]
    [MemberData(nameof(TimesOutOfRange))]
    public void A_failure_delay_out_of_range_is_refused(TimeSpan failureDelay)
    {
        AssertOutOfRange(nameof(WorkWorkerOptions.FailureDelay), new WorkWorkerOptions { FailureDelay = failureDelay });
    }

    [Theory]
    [MemberData(nameof(TimesInRange))]
    public void A_failure_delay_in_range_is_accepted(TimeSpan failureDelay)
    {
        CreateWorker(new WorkWorkerOptions { FailureDelay = failureDelay });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100001)]
    public void A_retention_batch_size_outside_1_to_100000_is_refused(int retentionBatchSize)
    {
        AssertOutOfRange(
            nameof(WorkWorkerOptions.RetentionBatchSize),
            new WorkWorkerOptions { RetentionBatchSize = retentionBatchSize });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100000)]
    public void A_retention_batch_size_of_1_to_100000_is_accepted(int retentionBatchSize)
    {
        CreateWorker(new WorkWorkerOptions { RetentionBatchSize = retentionBatchSize });
    }

    [Fact]
    public void A_null_time_provider_is_refused()
    {
        var options = new WorkWorkerOptions { TimeProvider = null! };

        Assert.Throws<ArgumentException>(nameof(WorkWorkerOptions.TimeProvider), () => CreateWorker(options));
    }

    private WorkWorker CreateWorker(WorkWorkerOptions options)
    {
        return new WorkWorker(_client, Balances, new CompletingHandler(), options);
    }

    private void AssertOutOfRange(string property, WorkWorkerOptions options)
    {
        Assert.Throws<ArgumentOutOfRangeException>(property, () => CreateWorker(options));
    }
}
