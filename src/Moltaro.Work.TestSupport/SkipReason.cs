namespace Moltaro.Work.TestSupport;

// The one reason with which a test of a behavior that is not implemented yet is skipped. The sprint that implements
// the behavior removes the skip and makes the test pass without changing what it asserts. A flaky test is never
// skipped with it.
public static class SkipReason
{
    public const string NotImplemented = "Not implemented yet.";
}
