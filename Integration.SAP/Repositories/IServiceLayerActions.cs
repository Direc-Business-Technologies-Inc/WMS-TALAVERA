using B1SLayer;

namespace Integration.Sap.Repositories;

public interface IServiceLayerActions
{
    string GetSapConnectionString();
    Task<T> GetAllAsync<T>(string module);
    Task<T> GetAsync<T>(string module, object id);
    Task<T> PostAsync<T, U>(string Module, U data);
    Task<U> PatchAsync<U>(string Module, dynamic Id, U data);
    Task<string> PatchStringAsync(string Module, dynamic Id, string data);
    Task<HttpResponseMessage[]> BatchAsync(SLBatchRequest[] sLBatchRequests);
    Task<List<T>> QueryAsync<T, U>(string fileName, U parameters);
    Task<List<T>> QueryAsync<T>(string fileName);
    List<T> Query<T, U>(string fileName, U parameters);
    List<T> Query<T>(string fileName);
    Task<List<Dictionary<string, object>>> QueryAsync(string fileName);
    T? Single<T>(string fileName);
    T? Single<T, U>(string fileName, U parameters);
    Task<T?> SingleAsync<T>(string fileName);
    Task<T?> SingleAsync<T, U>(string fileName, U parameters);
    Task<List<T>> RawQueryAsync<T>(string query);
    Task<T?> RawQueryOneAsync<T>(string query);
}
