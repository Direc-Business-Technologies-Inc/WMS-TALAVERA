using Database.Libraries.Repositories;

namespace Integration.NS.Helpers;

internal static class SuiteQLScriptResolver
{
    public static string ResolveSuiteQLScript(
        this ISqlQueryManager sqlQuery,
        string queryName,
        IReadOnlyDictionary<string, string>? parameters = null)
    {
        sqlQuery.GetSqlScriptWithMetadata(queryName, out string query, out bool isFound);

        if (!isFound)
        {
            throw new Exception($"SQL query '{queryName}' not found.");
        }

        // Substitution rules (including the {key} raw-splice guard) are shared
        // with NetSuiteApiClientService.NetsuiteQuery via SuiteQLText.
        return SuiteQLText.SubstituteScriptParameters(query, parameters);
    }
}
