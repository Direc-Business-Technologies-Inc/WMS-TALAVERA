using System.Data;
using System.Runtime.InteropServices;

namespace Database.Libraries.Repositories;

public interface ISqlQueryManager
{
    Dictionary<string, string> GetSqlScriptWithMetadata(string scriptFileName, out string sqlContent, [Optional] out bool isScriptFound, [Optional] string scriptFilePath);
    Dictionary<string, string> SetSqlScriptWithMetadata(string scriptFileName, string scriptContent, Dictionary<string, string> metadata, [Optional] string scriptFilePath);
    Task<int> ExecuteAsync<T>(string fileName, CommandType commandType, string connectionString);
    Task<int> ExecuteAsync<T, U>(string fileName, U Parameters, CommandType commandType, string connectionString);
    Task<List<T>> QueryAsync<T>(string fileName, CommandType commandType, string connectionString);
    Task<List<T>> QueryAsync<T, U>(string fileName, U Parameters, CommandType commandType, string connectionString);
    Task<List<T>> RawQueryAsync<T>(string query, CommandType commandType, string connectionString);
    Task<T?> RawQueryOneAsync<T>(string query, CommandType commandType, string connectionString);
    Task<List<T>> RawQueryAsync<T, U>(string query, U Parameters, CommandType commandType, string connectionString);
    Task<T?> QueryOneAsync<T>(string fileName, CommandType commandType, string connectionString);
    Task<T?> QueryOneAsync<T, U>(string fileName, U Parameters, CommandType commandType, string connectionString);
    Task<List<Dictionary<string, object>>> QueryAsync(string fileName, CommandType commandType, string connectionString);
}
