namespace Moltaro.Work.Tests.Workers;

// The checks of WorkClaim, which a provider creates when it claims an item.
public sealed class WorkClaimTests
{
    private static readonly WorkQueue Mail = new("mail", WorkShapeEnum.Log);

    [Fact]
    public void A_claim_keeps_its_item_and_its_fence()
    {
        var item = new WorkItem(Mail, 1, null, null, null, 1);

        var claim = new WorkClaim(item, fence: 1);

        Assert.Same(item, claim.Item);
        Assert.Equal(1, claim.Fence);
    }

    [Fact]
    public void A_claim_refuses_a_null_item()
    {
        Assert.Throws<ArgumentNullException>("item", () => new WorkClaim(null!, 1));
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void A_claim_refuses_a_fence_below_1(long fence)
    {
        var item = new WorkItem(Mail, 1, null, null, null, 1);

        Assert.Throws<ArgumentOutOfRangeException>("fence", () => new WorkClaim(item, fence));
    }
}
