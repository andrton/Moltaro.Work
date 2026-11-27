namespace Moltaro.Work.Tests.Workers;

// The checks of WorkItem, which a provider creates from a row when it claims an item.
public sealed class WorkItemTests
{
    private static readonly WorkQueue Mail = new("mail", WorkShapeEnum.Log);

    [Fact]
    public void An_item_keeps_what_it_was_created_with()
    {
        var item = new WorkItem(Mail, id: 7, key: "mail-7", scope: "customer-7", payload: "{}", attempt: 2);

        Assert.Same(Mail, item.Queue);
        Assert.Equal(7, item.Id);
        Assert.Equal("mail-7", item.Key);
        Assert.Equal("customer-7", item.Scope);
        Assert.Equal("{}", item.Payload);
        Assert.Equal(2, item.Attempt);
    }

    [Fact]
    public void An_item_keeps_a_null_key_scope_and_payload()
    {
        var item = new WorkItem(Mail, id: 1, key: null, scope: null, payload: null, attempt: 1);

        Assert.Null(item.Key);
        Assert.Null(item.Scope);
        Assert.Null(item.Payload);
    }

    [Fact]
    public void An_item_refuses_a_null_queue()
    {
        Assert.Throws<ArgumentNullException>("queue", () => new WorkItem(null!, 1, null, null, null, 1));
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void An_item_refuses_an_id_below_1(long id)
    {
        Assert.Throws<ArgumentOutOfRangeException>("id", () => new WorkItem(Mail, id, null, null, null, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void An_item_refuses_an_attempt_below_1(int attempt)
    {
        Assert.Throws<ArgumentOutOfRangeException>("attempt", () => new WorkItem(Mail, 1, null, null, null, attempt));
    }
}
