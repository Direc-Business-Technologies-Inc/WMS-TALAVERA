using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Database.Libraries.Helpers;

internal class SqlAsyncDataAccess
{

    public static async Task<int> ExecuteAsync(string query, string connectionString, CommandType command)
    {
        try
        {
            using IDbConnection connection = new SqlConnection(connectionString);

            var result = await connection.ExecuteAsync(
                query,
                commandType: command
            );
            return result;
        }
        catch (Exception)
        {

            throw;
        }
    }

    public static async Task<int> ExecuteAsync<U>(string query, U parameters, string connectionString, CommandType command)
    {
        try
        {
            using IDbConnection conn = new SqlConnection(connectionString);

            var result = await conn.ExecuteAsync(query, parameters, commandType: command);
            return result;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public static async Task<List<T>> GetDataAsync<T>(string query, string connectionString, CommandType command)
    {
        try
        {
            using IDbConnection conn = new SqlConnection(connectionString);
            var result = await conn.QueryAsync<T>(query, commandType: command);

            return result.AsList();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public static async Task<List<T>> GetDataAsync<T, U>(string query, U parameters, string connectionString, CommandType command)
    {
        try
        {
            using IDbConnection conn = new SqlConnection(connectionString);
            var result = await conn.QueryAsync<T>(query, parameters, commandType: command);

            return result.AsList();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public static async Task<T?> GetOneDataAsync<T>(string query, string connectionString, CommandType command)
    {
        try
        {
            using IDbConnection conn = new SqlConnection(connectionString);

            var result = await conn.QueryAsync<T>(query, commandType: command);
            return result.FirstOrDefault();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public static async Task<T?> GetOneDataAsync<T, U>(string query, U parameters, string connectionString, CommandType command)
    {
        try
        {
            using IDbConnection conn = new SqlConnection(connectionString);

            var result = await conn.QueryAsync<T>(query, parameters, commandType: command);
            return result.FirstOrDefault();
        }
        catch (Exception)
        {
            throw;
        }

    }

}
