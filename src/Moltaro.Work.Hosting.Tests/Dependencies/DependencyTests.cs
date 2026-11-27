using Moltaro.Work.TestSupport.Dependencies;

namespace Moltaro.Work.Hosting.Tests.Dependencies;

public sealed class DependencyTests
{
    [Fact]
    public void The_hosting_package_references_only_assemblies_of_dotnet_its_abstractions_Moltaro_Work_and_Moltaro_Signals()
    {
        string[] outside = ReferencedAssemblies.OutsideDotNet(typeof(WorkServiceCollectionExtensions).Assembly);

        Assert.Contains("Moltaro.Work", outside);
        Assert.All(outside, name => Assert.True(IsAllowed(name), $"The hosting package references {name}."));
    }

    // Principle 12: the core, Moltaro.Signals, which the core and the hosted workers use, and the .NET hosting
    // abstractions: the Microsoft.Extensions.*.Abstractions assemblies of hosting, dependency injection and logging,
    // which ship outside the shared framework. No implementation of them, such as Microsoft.Extensions.Hosting.
    private static bool IsAllowed(string name)
    {
        bool isAbstraction = name.StartsWith("Microsoft.Extensions.", StringComparison.Ordinal)
            && name.EndsWith(".Abstractions", StringComparison.Ordinal);

        return isAbstraction || name is "Moltaro.Work" or "Moltaro.Signals";
    }
}
