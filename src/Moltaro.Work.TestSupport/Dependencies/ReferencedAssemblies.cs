using System.Reflection;
using System.Runtime.InteropServices;

namespace Moltaro.Work.TestSupport.Dependencies;

// What an assembly references outside .NET, so that the test project of each library asserts its own set
// (principle 12). An assembly of .NET is one of the shared framework the test runs on; a package or another project of
// the repository is not there.
public static class ReferencedAssemblies
{
    // The names of the referenced assemblies that are not of .NET, in ordinal order.
    public static string[] OutsideDotNet(Assembly assembly)
    {
        string frameworkDirectory = RuntimeEnvironment.GetRuntimeDirectory();

        return assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .Where(name => !File.Exists(Path.Combine(frameworkDirectory, name + ".dll")))
            .Order(StringComparer.Ordinal)
            .ToArray();
    }
}
