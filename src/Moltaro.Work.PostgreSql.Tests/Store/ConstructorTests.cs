using Moltaro.Work.TestSupport.Doubles;
using Npgsql;

namespace Moltaro.Work.PostgreSql.Tests.Store;

// The checks of the constructor of PostgreSqlWorkStore.
public sealed class ConstructorTests
{
    [Fact]
    public void A_null_data_source_is_refused()
    {
        Assert.Throws<ArgumentNullException>("dataSource", () => new PostgreSqlWorkStore(null!));
    }

    [Fact]
    public void A_data_source_whose_connections_are_not_of_Npgsql_is_refused()
    {
        using var dataSource = new FakeDbDataSource();

        Assert.Throws<ArgumentException>("dataSource", () => new PostgreSqlWorkStore(dataSource));
    }

    // Nothing listens on port 1 of the loopback address, so a constructor that opened a connection would fail.
    [Fact]
    public async Task A_store_over_an_Npgsql_data_source_is_created_without_opening_a_connection()
    {
        await using NpgsqlDataSource dataSource = NpgsqlDataSource.Create(
            new NpgsqlConnectionStringBuilder { Host = "127.0.0.1", Port = 1 });

        _ = new PostgreSqlWorkStore(dataSource);
    }
}
