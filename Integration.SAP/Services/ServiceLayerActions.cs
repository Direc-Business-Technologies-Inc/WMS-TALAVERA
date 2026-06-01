using B1SLayer;
using Database.Libraries.Repositories;
using Integration.Sap.Repositories;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace Integration.Sap.Services;

public class ServiceLayerActions(
    IServiceLayer serviceLayer,
    ISqlQueryManager queryManager,
    IConfiguration configuration) 
    : IServiceLayerActions
{
    string CheckSAPConnection()
    {
        string SapConnectionStringName = Environment.GetEnvironmentVariable("SapConnectionStringName") ??
            throw new MissingMemberException(nameof(SapConnectionStringName));

        string? connString = configuration.GetConnectionString(SapConnectionStringName) ?? Environment.GetEnvironmentVariable($"ConnectionStrings_{SapConnectionStringName}") ?? throw new Exception($"Configuration does not have a connection named {SapConnectionStringName}");
        return connString;
    }

    SLRequest CreateRequest(string module, object? id = null) => id switch
    {
        null => serviceLayer.Access.Request(module),
        int => serviceLayer.Access.Request(module, id),
        string => serviceLayer.Access.Request(module, id),
        _ => throw new ArgumentException("Unknown type of id", nameof(id))
    };

    public string GetSapConnectionString() => CheckSAPConnection();

    public async Task<HttpResponseMessage[]> BatchAsync(SLBatchRequest[] sLBatchRequests)
    {
        try
        {
            HttpResponseMessage[] batchResult = await serviceLayer.Access.PostBatchAsync(sLBatchRequests);
            return batchResult;
        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task<T> GetAllAsync<T>(string module)
    {
        try
        {
            SLRequest request = CreateRequest(module);
            T data = await request.GetAsync<T>();
            return data;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<T> GetAsync<T>(string module, object id)
    {
        try
        {
            SLRequest? request = CreateRequest(module, id);
            T data = await request.GetAsync<T>();
            return data;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<U> PatchAsync<U>(string Module, dynamic Id, U data)
    {
        try
        {
            await serviceLayer.Access.Request(Module, Id).PatchAsync(data);
            return data;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<string> PatchStringAsync(string Module, dynamic Id, string data)
    {
        try
        {
            await serviceLayer.Access.Request(Module, Id).PatchStringAsync(data);
            return data;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<T> PostAsync<T, U>(string Module, U data)
    {
        try
        {
            T createdOrder = await serviceLayer.Access.Request(Module).PostAsync<T>(data);
            return createdOrder;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public List<T> Query<T, U>(string fileName, U parameters)
    {
        throw new NotImplementedException();
    }

    public List<T> Query<T>(string fileName)
    {
        throw new NotImplementedException();
    }

    public async Task<List<T>> QueryAsync<T, U>(string fileName, U parameters)
    {
        try
        {
            List<T> response = await queryManager.QueryAsync<T, U>(fileName, parameters, CommandType.Text, CheckSAPConnection());

            return response;

        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<List<T>> QueryAsync<T>(string fileName)
    {
        try
        {
            List<T> response = await queryManager.QueryAsync<T>(fileName, CommandType.Text, CheckSAPConnection());

            return response;

        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task<List<Dictionary<string, object>>> QueryAsync(string fileName)
    {
        try
        {
            return await queryManager.QueryAsync(fileName, CommandType.Text, CheckSAPConnection());
        }
        catch (Exception)
        {

            throw;
        }
    }

    public T? Single<T>(string fileName)
    {
        throw new NotImplementedException();
    }

    public T? Single<T, U>(string fileName, U parameters)
    {
        throw new NotImplementedException();
    }

    public async Task<T?> SingleAsync<T>(string fileName)
    {
        try
        {
            T? response = await queryManager.QueryOneAsync<T>(fileName, System.Data.CommandType.Text, CheckSAPConnection());

            return response;

        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<T?> SingleAsync<T, U>(string fileName, U parameters)
    {
        try
        {
            T? response = await queryManager.QueryOneAsync<T, U>(fileName, parameters, System.Data.CommandType.Text, CheckSAPConnection());

            return response;

        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<List<T>> RawQueryAsync<T>(string query)
    {
        try
        {
            List<T> response = await queryManager.RawQueryAsync<T>(query, CommandType.Text, CheckSAPConnection());

            return response;

        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task<T?> RawQueryOneAsync<T>(string query)
    {
        try
        {
            T? response = await queryManager.RawQueryOneAsync<T>(query, CommandType.Text, CheckSAPConnection());

            return response;

        }
        catch (Exception)
        {

            throw;
        }
    }
}
