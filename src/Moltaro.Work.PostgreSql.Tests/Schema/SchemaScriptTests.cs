using System.Data.Common;
using Moltaro.Work.PostgreSql.Tests.Database;

namespace Moltaro.Work.PostgreSql.Tests.Schema;

// The schema script of version 1. The database of the run was created empty and given the script
// (PostgreSqlDatabase), so the run itself applies the script to an empty database.
public sealed class SchemaScriptTests
{
    private readonly PostgreSqlDatabase _database;

    public SchemaScriptTests(PostgreSqlDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task The_script_applies_to_an_empty_database_and_records_its_version_which_is_SchemaVersion()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using DbConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = "SELECT version FROM moltaro_work_schema";

        var versions = new List<int>();

        await using (DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                versions.Add(reader.GetInt32(0));
            }
        }

        Assert.Equal([1], versions);
        Assert.Equal(1, PostgreSqlWorkStore.SchemaVersion);
    }
}
