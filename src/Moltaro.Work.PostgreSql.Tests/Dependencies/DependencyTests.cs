using Moltaro.Work.TestSupport.Dependencies;

namespace Moltaro.Work.PostgreSql.Tests.Dependencies;

public sealed class DependencyTests
{
    [Fact]
    public void The_provider_references_only_assemblies_of_dotnet_Moltaro_Work_and_Npgsql()
    {
        Assert.Equal(
            ["Moltaro.Work", "Npgsql"],
            ReferencedAssemblies.OutsideDotNet(typeof(PostgreSqlWorkStore).Assembly));
    }
}
