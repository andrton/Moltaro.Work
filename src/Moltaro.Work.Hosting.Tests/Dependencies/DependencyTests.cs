using Moltaro.Work.TestSupport.Dependencies;

namespace Moltaro.Work.Hosting.Tests.Dependencies;

public sealed class DependencyTests
{
    [Fact]
    public void The_hosting_package_references_only_assemblies_of_dotnet_Microsoft_Extensions_Moltaro_Work_and_Moltaro_Signals()
    {
        string[] outside = ReferencedAssemblies.OutsideDotNet(typeof(WorkServiceCollectionExtensions).Assembly);

        Assert.Contains("Moltaro.Work", outside);
        Assert.All(outside, name => Assert.True(IsAllowed(name), $"The hosting package references {name}."));
    }

    // Principle 12: the core, Moltaro.Signals, which the core and the hosted workers use, and the abstractions of .NET's
    // hosting, dependency injection and logging, which ship outside the shared framework.
    private static bool IsAllowed(string name)
    {
        return name is "Moltaro.Work" or "Moltaro.Signals"
            || name.StartsWith("Microsoft.Extensions.", StringComparison.Ordinal);
    }
}
