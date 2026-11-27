using Moltaro.Work.TestSupport.Dependencies;

namespace Moltaro.Work.Tests.Dependencies;

public sealed class DependencyTests
{
    [Fact]
    public void The_core_references_only_assemblies_of_dotnet_and_Moltaro_Signals()
    {
        Assert.Equal(["Moltaro.Signals"], ReferencedAssemblies.OutsideDotNet(typeof(WorkClient).Assembly));
    }
}
