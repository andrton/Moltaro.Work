namespace Moltaro.Work.Tests.Queues;

// The rule of a queue name and the shape, checked by the constructor of WorkQueue.
public sealed class WorkQueueTests
{
    public static TheoryData<string> BrokenNames =>
    [
        "",
        new string('q', 101),
        "mail.sent",
        "mail sent",
        "naïve",
        "mail:sent",
    ];

    public static TheoryData<string> GoodNames =>
    [
        "q",
        new string('q', 100),
        "ABCXYZ-abcxyz_0189",
        "-",
        "_",
    ];

    [Fact]
    public void A_null_name_is_refused()
    {
        Assert.Throws<ArgumentNullException>("name", () => new WorkQueue(null!, WorkShapeEnum.Log));
    }

    [Theory]
    [MemberData(nameof(BrokenNames))]
    public void A_name_that_is_not_1_to_100_characters_of_ASCII_letters_digits_underscore_and_hyphen_is_refused(string name)
    {
        Assert.Throws<ArgumentException>("name", () => new WorkQueue(name, WorkShapeEnum.Log));
    }

    [Theory]
    [MemberData(nameof(GoodNames))]
    public void A_name_of_1_to_100_characters_of_ASCII_letters_digits_underscore_and_hyphen_is_kept(string name)
    {
        var queue = new WorkQueue(name, WorkShapeEnum.Log);

        Assert.Equal(name, queue.Name);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    public void A_shape_that_is_not_defined_is_refused(int shape)
    {
        Assert.Throws<ArgumentOutOfRangeException>("shape", () => new WorkQueue("mail", (WorkShapeEnum)shape));
    }

    [Theory]
    [InlineData(WorkShapeEnum.Coalescing)]
    [InlineData(WorkShapeEnum.Log)]
    [InlineData(WorkShapeEnum.Run)]
    public void Every_shape_is_kept(WorkShapeEnum shape)
    {
        var queue = new WorkQueue("mail", shape);

        Assert.Equal(shape, queue.Shape);
    }
}
