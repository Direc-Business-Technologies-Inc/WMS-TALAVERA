using Dapper;
using Database.Libraries.Helpers;
using Database.Libraries.Repositories;
using Microsoft.Data.SqlClient;
using Shared.Kernel;
using System.Data;
using System.IO;
using System.Runtime.InteropServices;

namespace Database.Libraries.DataAccess;

public class SqlQueryManager
    : ISqlQueryManager
{

    public Dictionary<string, string> GetSqlScriptWithMetadata(string scriptFileName, out string sqlContent, [Optional] out bool isScriptFound, [Optional] string scriptFilePath)
    {
        try
        {
            Dictionary<string, string> metadata = new();

            string filePath = string.Empty;
            string basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts");

            if (!string.IsNullOrEmpty(scriptFilePath))
            {
                basePath = Path.Combine(basePath, scriptFilePath);

                if (!Directory.Exists(basePath))
                    Directory.CreateDirectory(basePath);
            }

            filePath = Path.Combine(basePath, $"{scriptFileName}.sql");

            if (File.Exists(filePath) == true)
            {
                isScriptFound = true;

                using (StreamReader objReader = new StreamReader(filePath))
                {
                    var data = objReader.ReadToEnd();
                    isScriptFound = true;

                    int metadataStart = data.IndexOf("-- METADATA_BEGIN");
                    int metadataEnd = data.IndexOf("-- METADATA_END");

                    if (metadataStart >= 0 && metadataEnd > metadataStart)
                    {
                        string metadataSection = data.Substring(
                            metadataStart,
                            metadataEnd - metadataStart);

                        using (StringReader metadataReader = new StringReader(metadataSection))
                        {
                            string line;

                            while ((line = metadataReader.ReadLine()) != null)
                            {
                                if (line.StartsWith("-- ") && line != "-- METADATA_BEGIN")
                                {
                                    string metadataLine = line.Substring(3);

                                    int colonIndex = metadataLine.IndexOf(':');
                                    if (colonIndex > 0)
                                    {
                                        string key = metadataLine.Substring(0, colonIndex).Trim();
                                        string value = metadataLine.Substring(colonIndex + 1).Trim();

                                        if (!key.Equals("METADATA_BEGIN") && !key.Equals("METADATA_END"))
                                        {
                                            metadata[key] = value;
                                        }
                                    }
                                }
                            }
                        }

                        sqlContent = data.Substring(data.IndexOf("*/", metadataEnd) + 2).Trim();

                    }
                    else
                    {
                        sqlContent = data;
                    }

                    return metadata;
                }
            }
            else
            {
                isScriptFound = false;
                sqlContent = string.Empty;
                return [];
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    public Dictionary<string, string> SetSqlScriptWithMetadata(string scriptFileName, string scriptContent, Dictionary<string, string> metadata, [Optional] string scriptFilePath)
    {
        throw new NotImplementedException();
    }

    public async Task<int> ExecuteAsync<T>(string fileName, CommandType commandType, string connectionString)
    {
        try
        {
            Dictionary<string, string> qryDetails = GetSqlScriptWithMetadata(fileName, out string sqlContent, out bool found);

            if (!found)
                throw new Exception($"SQL Script {fileName} not found in local scripts.");

            return await SqlAsyncDataAccess.ExecuteAsync(sqlContent, connectionString, CommandType.Text);
        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task<int> ExecuteAsync<T, U>(string fileName, U Parameters, CommandType commandType, string connectionString)
    {
        try
        {
            Dictionary<string, string> qryDetails = GetSqlScriptWithMetadata(fileName, out string sqlContent, out bool found);

            if (!found)
                throw new Exception($"SQL Script {fileName} not found in local scripts.");

            return await SqlAsyncDataAccess.ExecuteAsync(sqlContent, Parameters, connectionString, commandType);
        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task<List<T>> QueryAsync<T>(string fileName, CommandType commandType, string connectionString)
    {
        try
        {
            Dictionary<string, string> qryDetails = GetSqlScriptWithMetadata(fileName, out string sqlContent, out bool found);

            if (!found)
                throw new Exception($"SQL Script {fileName} not found in local scripts.");

            return await SqlAsyncDataAccess.GetDataAsync<T>(sqlContent, connectionString, commandType);
        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task<List<T>> QueryAsync<T, U>(string fileName, U Parameters, CommandType commandType, string connectionString)
    {
        try
        {
            Dictionary<string, string> qryDetails = GetSqlScriptWithMetadata(fileName, out string sqlContent, out bool found);

            if (!found)
                throw new Exception($"SQL Script {fileName} not found in local scripts.");

            return await SqlAsyncDataAccess.GetDataAsync<T, U>(sqlContent, Parameters, connectionString, commandType);
        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task<List<Dictionary<string, object>>> QueryAsync(string fileName, CommandType commandType, string connectionString)
    {
        Dictionary<string, string> qryDetails = GetSqlScriptWithMetadata(fileName, out string sqlContent, out bool found);

        if (!found)
            throw new Exception($"SQL Script {fileName} not found in local scripts.");

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var result = await connection.QueryAsync(sqlContent, commandType: commandType);
        List<Dictionary<string, object>> resultDT = AppTypeConverter.QueryToDictionary(result);

        return resultDT;
    }

    public async Task<T?> QueryOneAsync<T>(string fileName, CommandType commandType, string connectionString)
    {
        try
        {
            Dictionary<string, string> qryDetails = GetSqlScriptWithMetadata(fileName, out string sqlContent, out bool found);

            if (!found)
                throw new Exception($"SQL Script {fileName} not found in local scripts.");

            return await SqlAsyncDataAccess.GetOneDataAsync<T>(sqlContent, connectionString, commandType);
        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task<T?> QueryOneAsync<T, U>(string fileName, U Parameters, CommandType commandType, string connectionString)
    {
        try
        {
            Dictionary<string, string> qryDetails = GetSqlScriptWithMetadata(fileName, out string sqlContent, out bool found);

            if (!found)
                throw new Exception($"SQL Script {fileName} not found in local scripts.");

            return await SqlAsyncDataAccess.GetOneDataAsync<T, U>(sqlContent, Parameters, connectionString, commandType);
        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task<List<T>> RawQueryAsync<T>(string query, CommandType commandType, string connectionString)
    {
        try
        {
            return await SqlAsyncDataAccess.GetDataAsync<T>(query, connectionString, commandType);
        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task<List<T>> RawQueryAsync<T, U>(string query, U Parameters, CommandType commandType, string connectionString)
    {
        try
        {
            return await SqlAsyncDataAccess.GetDataAsync<T, U>(query, Parameters, connectionString, commandType);
        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task<T?> RawQueryOneAsync<T>(string query, CommandType commandType, string connectionString)
    {
        try
        {
            return await SqlAsyncDataAccess.GetOneDataAsync<T>(query, connectionString, commandType);
        }
        catch (Exception)
        {

            throw;
        }
    }
}
