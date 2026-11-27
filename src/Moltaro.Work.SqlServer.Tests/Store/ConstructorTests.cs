using System.Data.Common;
using Microsoft.Data.SqlClient;
using Moltaro.Work.TestSupport.Doubles;

namespace Moltaro.Work.SqlServer.Tests.Store;

// The checks of the constructor of SqlServerWorkStore.
public sealed class ConstructorTests
{
    [Fact]
    public void A_null_data_source_is_refused()
    {
        Assert.Throws<ArgumentNullException>("dataSource", () => new SqlServerWorkStore(null!));
    }

    [Fact]
    public void A_data_source_whose_connections_are_not_of_Microsoft_Data_SqlClient_is_refused()
    {
        using var dataSource = new FakeDbDataSource();

        Assert.Throws<ArgumentException>("dataSource", () => new SqlServerWorkStore(dataSource));
    }

    // Nothing listens on port 1 of the loopback address, so a constructor that opened a connection would fail.
    [Fact]
    public async Task A_store_over_a_SqlClient_data_source_is_created_without_opening_a_connection()
    {
        await using DbDataSource dataSource = SqlClientFactory.Instance.CreateDataSource(
            new SqlConnectionStringBuilder { DataSource = "127.0.0.1,1" }.ConnectionString);

        _ = new SqlServerWorkStore(dataSource);
    }
}
