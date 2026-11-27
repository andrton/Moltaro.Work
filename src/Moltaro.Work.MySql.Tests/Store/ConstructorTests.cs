using Moltaro.Work.TestSupport.Doubles;
using MySqlConnector;

namespace Moltaro.Work.MySql.Tests.Store;

// The checks of the constructor of MySqlWorkStore.
public sealed class ConstructorTests
{
    [Fact]
    public void A_null_data_source_is_refused()
    {
        Assert.Throws<ArgumentNullException>("dataSource", () => new MySqlWorkStore(null!));
    }

    [Fact]
    public void A_data_source_whose_connections_are_not_of_MySqlConnector_is_refused()
    {
        using var dataSource = new FakeDbDataSource();

        Assert.Throws<ArgumentException>("dataSource", () => new MySqlWorkStore(dataSource));
    }

    // Nothing listens on port 1 of the loopback address, so a constructor that opened a connection would fail.
    [Fact]
    public async Task A_store_over_a_MySqlConnector_data_source_is_created_without_opening_a_connection()
    {
        await using var dataSource = new MySqlDataSource(
            new MySqlConnectionStringBuilder { Server = "127.0.0.1", Port = 1 }.ConnectionString);

        _ = new MySqlWorkStore(dataSource);
    }
}
