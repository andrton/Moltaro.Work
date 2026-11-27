using Moltaro.Work.TestSupport.Dependencies;

namespace Moltaro.Work.MySql.Tests.Dependencies;

public sealed class DependencyTests
{
    [Fact]
    public void The_provider_references_only_assemblies_of_dotnet_Moltaro_Work_and_MySqlConnector()
    {
        Assert.Equal(
            ["Moltaro.Work", "MySqlConnector"],
            ReferencedAssemblies.OutsideDotNet(typeof(MySqlWorkStore).Assembly));
    }
}
