using System.Data.Common;

namespace Moltaro.Work.Hosting.Tests.Doubles;

// The driver's error of a database that cannot be reached, which HandOutWorkStore reports for a claim.
internal sealed class UnreachableDatabaseException : DbException
{
    public UnreachableDatabaseException()
        : base("The database of the test cannot be reached.")
    {
    }
}
