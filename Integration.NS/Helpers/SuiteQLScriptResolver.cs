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

        if (parameters is null)
        {
            return query;
        }

        foreach (var parameter in parameters)
        {
            query = query.Replace(
                $"@{parameter.Key}",
                $"'{parameter.Value.Replace("'", "''")}'");

            query = query.Replace(
                $"{{{parameter.Key}}}",
                parameter.Value);
        }

        return query;
    }
}
