using Moltaro.Signals;
using Moltaro.Work.Tests.Doubles;
using Moltaro.Work.TestSupport.Doubles;

namespace Moltaro.Work.Tests.Enqueue;

// The checks of WorkTransaction.EnqueueAsync, in their order: each test breaks the check it names and every later
// one, so the exception shows that its check ran first, and the store shows that nothing reached it.
public sealed class EnqueueTests : IAsyncDisposable
{
    private static readonly WorkQueue Balances = new("balances", WorkShapeEnum.Coalescing);

    private static readonly WorkQueue Mail = new("mail", WorkShapeEnum.Log);

    private static readonly WorkQueue Imports = new("imports", WorkShapeEnum.Run, new WorkQueueOptions { Scoped = true });

    private static readonly CancellationToken Cancelled = new(canceled: true);

    private readonly SignalHost _signals = LoopbackSignalHost.Create();

    private readonly RecordingWorkStore _store = new();

    private readonly FakeDbTransaction _dbTransaction = new(new FakeDbConnection());

    private readonly WorkTransaction _transaction;

    public EnqueueTests()
    {
        _transaction = new WorkClient(_store, _signals).Join(_dbTransaction);
    }

    public static TheoryData<string> BrokenKeys =>
    [
        "",
        new string('k', 201),
        "line\nbreak",
        "delete\u007F",
        "next\u0085line",
        "high\uD800",
        "\uDC00low",
        "space ",
    ];

    public static TheoryData<string> GoodKeys =>
    [
        "k",
        new string('k', 200),
        " leading",
        "two words",
        "Größe",
        "pair😀",
    ];

    public static TheoryData<string> BrokenPayloads =>
    [
        "\0",
        "{\"text\":\"a\0b\"}",
        "high\uD800",
        "\uDC00low",
    ];

    public static TheoryData<string> GoodPayloads =>
    [
        "",
        "{\"text\":\"hello\"}",
        "pair😀",
        "line\nbreak\t\u007F",
    ];

    public ValueTask DisposeAsync()
    {
        return _signals.DisposeAsync();
    }

    [Fact]
    public void A_null_queue_is_refused_first()
    {
        CallCommit();

        Assert.Throws<ArgumentNullException>(
            "queue",
            () => { _ = _transaction.EnqueueAsync(null!, key: "", cancellationToken: Cancelled); });

        Assert.Empty(_store.EnqueueCalls);
    }

    [Fact]
    public void An_enqueue_after_CommitAsync_was_called_is_refused_before_the_arguments_are_checked()
    {
        CallCommit();

        Assert.Throws<InvalidOperationException>(
            () => { _ = _transaction.EnqueueAsync(Balances, key: null, payload: "{}", cancellationToken: Cancelled); });

        Assert.Empty(_store.EnqueueCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task An_enqueue_after_one_whose_task_faulted_or_was_cancelled_is_refused_before_the_arguments_are_checked(
        bool cancelled)
    {
        await FailOneEnqueueAsync(cancelled);

        Assert.Throws<InvalidOperationException>(
            () => { _ = _transaction.EnqueueAsync(Balances, key: null, payload: "{}", cancellationToken: Cancelled); });

        Assert.Single(_store.EnqueueCalls);
    }

    [Fact]
    public void An_enqueue_in_a_transaction_that_has_ended_is_refused_before_the_arguments_are_checked()
    {
        _dbTransaction.Rollback();

        Assert.Throws<InvalidOperationException>(
            () => { _ = _transaction.EnqueueAsync(Balances, key: null, payload: "{}", cancellationToken: Cancelled); });

        Assert.Empty(_store.EnqueueCalls);
    }

    [Fact]
    public void A_coalescing_item_without_a_key_is_refused()
    {
        Assert.Throws<ArgumentException>(
            "key",
            () => { _ = _transaction.EnqueueAsync(Balances, key: null, scope: "", payload: "{}", cancellationToken: Cancelled); });

        Assert.Empty(_store.EnqueueCalls);
    }

    [Fact]
    public void A_coalescing_item_with_a_payload_is_refused()
    {
        Assert.Throws<ArgumentException>(
            "payload",
            () => { _ = _transaction.EnqueueAsync(Balances, key: "", scope: "", payload: "{}", cancellationToken: Cancelled); });

        Assert.Empty(_store.EnqueueCalls);
    }

    [Theory]
    [MemberData(nameof(BrokenKeys))]
    public void A_key_that_breaks_the_rule_is_refused(string key)
    {
        Assert.Throws<ArgumentException>(
            "key",
            () => { _ = _transaction.EnqueueAsync(Mail, key: key, scope: "", payload: "\0", cancellationToken: Cancelled); });

        Assert.Empty(_store.EnqueueCalls);
    }

    [Theory]
    [MemberData(nameof(BrokenKeys))]
    public void A_scope_that_breaks_the_rule_of_a_key_is_refused(string scope)
    {
        Assert.Throws<ArgumentException>(
            "scope",
            () => { _ = _transaction.EnqueueAsync(Imports, scope: scope, payload: "\0", cancellationToken: Cancelled); });

        Assert.Empty(_store.EnqueueCalls);
    }

    [Fact]
    public void A_scope_for_a_queue_that_is_not_scoped_is_refused()
    {
        Assert.Throws<ArgumentException>(
            "scope",
            () => { _ = _transaction.EnqueueAsync(Mail, scope: "customer-7", payload: "\0", cancellationToken: Cancelled); });

        Assert.Empty(_store.EnqueueCalls);
    }

    [Theory]
    [MemberData(nameof(BrokenPayloads))]
    public void A_payload_that_breaks_its_rule_is_refused(string payload)
    {
        Assert.Throws<ArgumentException>(
            "payload",
            () => { _ = _transaction.EnqueueAsync(Mail, payload: payload, cancellationToken: Cancelled); });

        Assert.Empty(_store.EnqueueCalls);
    }

    [Fact]
    public async Task A_token_that_is_already_cancelled_gives_a_cancelled_task_and_the_store_is_not_called()
    {
        Task<WorkEnqueueResult> enqueue = _transaction.EnqueueAsync(Balances, key: "invoice-7", cancellationToken: Cancelled);

        Assert.True(enqueue.IsCanceled);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => enqueue);
        Assert.Empty(_store.EnqueueCalls);
    }

    [Fact]
    public async Task Valid_arguments_reach_the_store_unchanged_and_its_result_is_returned()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var dueAt = new DateTimeOffset(2026, 11, 27, 9, 30, 0, TimeSpan.FromHours(1));
        var stored = new WorkEnqueueResult(WorkEnqueueStatusEnum.Coalesced, 42);
        _store.EnqueueResult = Task.FromResult(stored);

        WorkEnqueueResult result = await _transaction.EnqueueAsync(
            Imports,
            key: "import-7",
            scope: "customer-7",
            payload: "{\"file\":\"7.csv\"}",
            dueAt: dueAt,
            cancellationToken: cancellationToken);

        Assert.Equal(stored, result);
        EnqueueCall call = Assert.Single(_store.EnqueueCalls);
        Assert.Same(_dbTransaction, call.Transaction);
        Assert.Same(Imports, call.Queue);
        Assert.Equal("import-7", call.Key);
        Assert.Equal("customer-7", call.Scope);
        Assert.Equal("{\"file\":\"7.csv\"}", call.Payload);
        Assert.Equal(dueAt, call.DueAt);
        Assert.Equal(dueAt.Offset, call.DueAt!.Value.Offset);
        Assert.Equal(cancellationToken, call.CancellationToken);
    }

    [Fact]
    public async Task A_coalescing_item_with_a_key_and_nothing_else_reaches_the_store()
    {
        await _transaction.EnqueueAsync(Balances, key: "invoice-7", cancellationToken: TestContext.Current.CancellationToken);

        EnqueueCall call = Assert.Single(_store.EnqueueCalls);
        Assert.Equal("invoice-7", call.Key);
        Assert.Null(call.Scope);
        Assert.Null(call.Payload);
        Assert.Null(call.DueAt);
    }

    [Fact]
    public async Task A_log_item_without_a_key_reaches_the_store()
    {
        await _transaction.EnqueueAsync(Mail, payload: "{}", cancellationToken: TestContext.Current.CancellationToken);

        EnqueueCall call = Assert.Single(_store.EnqueueCalls);
        Assert.Null(call.Key);
    }

    [Theory]
    [MemberData(nameof(GoodKeys))]
    public async Task A_key_or_a_scope_that_fits_the_rule_reaches_the_store(string value)
    {
        await _transaction.EnqueueAsync(Imports, key: value, scope: value, cancellationToken: TestContext.Current.CancellationToken);

        EnqueueCall call = Assert.Single(_store.EnqueueCalls);
        Assert.Equal(value, call.Key);
        Assert.Equal(value, call.Scope);
    }

    [Theory]
    [MemberData(nameof(GoodPayloads))]
    public async Task A_payload_that_fits_its_rule_reaches_the_store(string payload)
    {
        await _transaction.EnqueueAsync(Mail, payload: payload, cancellationToken: TestContext.Current.CancellationToken);

        EnqueueCall call = Assert.Single(_store.EnqueueCalls);
        Assert.Equal(payload, call.Payload);
    }

    [Fact]
    public async Task Every_enqueue_of_a_transaction_reaches_the_store_while_none_failed()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await _transaction.EnqueueAsync(Balances, key: "invoice-7", cancellationToken: cancellationToken);
        await _transaction.EnqueueAsync(Mail, payload: "{}", cancellationToken: cancellationToken);

        Assert.Equal(2, _store.EnqueueCalls.Count);
    }

    [Fact]
    public async Task An_enqueue_whose_token_was_already_cancelled_leaves_the_transaction_open_for_enqueues_and_the_commit()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Task<WorkEnqueueResult> cancelled = _transaction.EnqueueAsync(Balances, key: "invoice-7", cancellationToken: Cancelled);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled);

        await _transaction.EnqueueAsync(Balances, key: "invoice-7", cancellationToken: cancellationToken);

        Assert.Single(_store.EnqueueCalls);
        CallCommit();
    }

    [Fact]
    public async Task An_enqueue_refused_by_an_argument_check_leaves_the_transaction_open_for_enqueues_and_the_commit()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Assert.Throws<ArgumentException>(
            "key",
            () => { _ = _transaction.EnqueueAsync(Balances, key: null, cancellationToken: cancellationToken); });

        await _transaction.EnqueueAsync(Balances, key: "invoice-7", cancellationToken: cancellationToken);

        Assert.Single(_store.EnqueueCalls);
        CallCommit();
    }

    // A call with a token that is already cancelled passes the checks of CommitAsync and counts, and gives a cancelled
    // task in every stage: the cancelled task shows that its checks passed.
    private void CallCommit()
    {
        Task commit = _transaction.CommitAsync(Cancelled);

        Assert.True(commit.IsCanceled);
    }

    private async Task FailOneEnqueueAsync(bool cancelled)
    {
        _store.EnqueueResult = cancelled
            ? Task.FromCanceled<WorkEnqueueResult>(Cancelled)
            : Task.FromException<WorkEnqueueResult>(new TimeoutException("The database did not answer."));

        Task<WorkEnqueueResult> enqueue = _transaction.EnqueueAsync(
            Balances,
            key: "invoice-7",
            cancellationToken: TestContext.Current.CancellationToken);

        if (cancelled)
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => enqueue);
        }
        else
        {
            await Assert.ThrowsAsync<TimeoutException>(() => enqueue);
        }
    }
}
