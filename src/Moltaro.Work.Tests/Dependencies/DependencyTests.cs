using System.Runtime.InteropServices;

namespace Moltaro.Work.Tests.Dependencies;

public sealed class DependencyTests
{
    // An assembly of .NET is one of the shared framework this test runs on; a package or another project of the
    // repository is not there.
    [Fact]
    public void The_core_references_only_assemblies_of_dotnet_and_Moltaro_Signals()
    {
        string frameworkDirectory = RuntimeEnvironment.GetRuntimeDirectory();

        string[] others = typeof(WorkClient).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .Where(name => !File.Exists(Path.Combine(frameworkDirectory, name + ".dll")))
            .ToArray();

        Assert.Equal(["Moltaro.Signals"], others);
    }
}
