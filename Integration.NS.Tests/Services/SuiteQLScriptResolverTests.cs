using System.Data;
using Database.Libraries.Repositories;
using Integration.NS.Helpers;
using Integration.NS.Tests.Asserts;
using Xunit;

namespace Integration.NS.Tests.Services;

/// <summary>
/// End-to-end coverage for the named-script chokepoint: a script is loaded from
/// a (fake) <see cref="ISqlQueryManager"/>, its parameters are substituted by
/// <c>SuiteQLScriptResolver.ResolveSuiteQLScript</c> (delegating to
/// <c>SuiteQLText</c>), and the resolved query must pass every
/// <see cref="SuiteQLAssert"/> invariant. Until this suite existed the entire
/// named-script path was untested — the Capturing fakes throw
/// NotSupportedException for NetsuiteQuery.
/// </summary>
public class SuiteQLScriptResolverTests
{
    private sealed class FakeSqlQueryManager : ISqlQueryManager
    {
        public string Script { get; set; } = string.Empty;
        public bool Found { get; set; } = true;

        public Dictionary<string, string> GetSqlScriptWithMetadata(
            string scriptFileName, out string sqlContent, out bool isScriptFound, string scriptFilePath = "")
        {
            sqlContent = Script;
            isScriptFound = Found;
            return [];
        }

        public Dictionary<string, string> SetSqlScriptWithMetadata(string scriptFileName, string scriptContent, Dictionary<string, string> metadata, string scriptFilePath = "")
            => throw new NotSupportedException();
        public Task<int> ExecuteAsync<T>(string fileName, CommandType commandType, string connectionString)
            => throw new NotSupportedException();
        public Task<int> ExecuteAsync<T, U>(string fileName, U parameters, CommandType commandType, string connectionString)
            => throw new NotSupportedException();
        public Task<List<T>> QueryAsync<T>(string fileName, CommandType commandType, string connectionString)
            => throw new NotSupportedException();
        public Task<List<T>> QueryAsync<T, U>(string fileName, U parameters, CommandType commandType, string connectionString)
            => throw new NotSupportedException();
        public Task<List<T>> RawQueryAsync<T>(string query, CommandType commandType, string connectionString)
            => throw new NotSupportedException();
        public Task<T?> RawQueryOneAsync<T>(string query, CommandType commandType, string connectionString)
            => throw new NotSupportedException();
        public Task<List<T>> RawQueryAsync<T, U>(string query, U parameters, CommandType commandType, string connectionString)
            => throw new NotSupportedException();
        public Task<T?> QueryOneAsync<T>(string fileName, CommandType commandType, string connectionString)
            => throw new NotSupportedException();
        public Task<T?> QueryOneAsync<T, U>(string fileName, U parameters, CommandType commandType, string connectionString)
            => throw new NotSupportedException();
        public Task<List<Dictionary<string, object>>> QueryAsync(string fileName, CommandType commandType, string connectionString)
            => throw new NotSupportedException();
    }

    [Fact]
    public void Substituted_script_passes_every_invariant()
    {
        var manager = new FakeSqlQueryManager
        {
            Script = "SELECT i.id FROM item i WHERE i.id IN ({items}) AND bc.name = @barcode"
        };

        var resolved = manager.ResolveSuiteQLScript("NS_Item_Get_Barcodes", new Dictionary<string, string>
        {
            ["items"] = "7,9",
            ["barcode"] = "O'Reilly"
        });

        Assert.Contains("i.id IN (7,9)", resolved);
        Assert.Contains("bc.name = 'O''Reilly'", resolved);
        SuiteQLAssert.WellFormed(resolved);
    }

    [Fact]
    public void Null_parameters_return_the_raw_script()
    {
        const string script = "SELECT 1 FROM location l";
        var manager = new FakeSqlQueryManager { Script = script };

        var resolved = manager.ResolveSuiteQLScript("NS_Get_Locations", null);

        Assert.Equal(script, resolved);
    }

    [Fact]
    public void Missing_script_throws_with_the_script_name()
    {
        var manager = new FakeSqlQueryManager { Found = false };

        var exception = Assert.Throws<Exception>(() =>
            manager.ResolveSuiteQLScript("NS_Does_Not_Exist"));

        Assert.Contains("NS_Does_Not_Exist", exception.Message);
    }

    [Fact]
    public void Empty_selection_for_a_bare_parameter_fails_locally_not_at_netsuite()
    {
        // Mirrors the real NS_Location_Get_Bins.sql WHERE clause shape
        // (b.location IN ({locations})) with an empty Location selection —
        // previously this shipped 'IN ()' and NetSuite answered 400.
        var manager = new FakeSqlQueryManager
        {
            Script = "SELECT b.id FROM bin b WHERE b.location IN ({locations})"
        };

        Assert.Throws<InvalidOperationException>(() =>
            manager.ResolveSuiteQLScript("NS_Location_Get_Bins", new Dictionary<string, string>
            {
                ["locations"] = string.Join(",", Array.Empty<int>())
            }));
    }
}
