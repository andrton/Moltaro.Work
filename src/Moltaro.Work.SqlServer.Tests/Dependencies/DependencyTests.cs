using Moltaro.Work.TestSupport.Dependencies;

namespace Moltaro.Work.SqlServer.Tests.Dependencies;

public sealed class DependencyTests
{
    [Fact]
    public void The_provider_references_only_assemblies_of_dotnet_Moltaro_Work_and_Microsoft_Data_SqlClient()
    {
        Assert.Equal(
            ["Microsoft.Data.SqlClient", "Moltaro.Work"],
            ReferencedAssemblies.OutsideDotNet(typeof(SqlServerWorkStore).Assembly));
    }
}
