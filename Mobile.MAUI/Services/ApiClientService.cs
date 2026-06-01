using Mobile.MAUI.Helpers;
using Mobile.MAUI.ViewModel;
using RestSharp;
using Shared.Libraries.Entities;
using System.Text.Json;

namespace Mobile.MAUI.Services;

public class ApiClientService
{
    RestClient? Client { get; set; }

    public ApiClientService()
    {
        
    }
    public void UpdateClient(ClientEndpointVM vm)
    {
        string url = EndpointHelper.BuildURI(vm);
        Client = new RestClient(new RestClientOptions { BaseUrl = new Uri(url) });
    }
    public async Task<ApiResult<T>> Get<T>(string endpoint, object? body = null)
    {
        if (Client is null) throw new InvalidOperationException("Client is not initialized. Check your endpoint settings first.");

        RestRequest request = new RestRequest(endpoint, Method.Get);

        if (body is not null)
        {
            string jsonString = JsonSerializer.Serialize(body);
            request.AddJsonBody(jsonString);
        }

        string? token = await SecureStorage.GetAsync("access-token");
        if (!string.IsNullOrEmpty(token)) request.AddHeader("Authorization", $"Bearer {token ?? ""}");
        RestResponse res = await Client.ExecuteAsync(request);
        if (res.StatusCode == System.Net.HttpStatusCode.NotFound)
        {

            throw new Exception("404. The endpoint does not exist.");
        }
        else if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new Exception("Not authorized.");
        }

        return (!string.IsNullOrEmpty(res.Content) ? JsonSerializer.Deserialize<ApiResult<T>>(res.Content) : new())!;
    }
    public async Task<ApiResult<T>> Post<T>(string endpoint, object? body = null)
    {
        if (Client is null) throw new InvalidOperationException("Client is not initialized. Check your endpoint settings first.");
        RestRequest request = new RestRequest(endpoint, Method.Post);

        if (body != null)
        {
            string jsonString = JsonSerializer.Serialize(body);
            request.AddJsonBody(jsonString);
        }

        string? token = await SecureStorage.GetAsync("access-token");
        if (!string.IsNullOrEmpty(token)) request.AddHeader("Authorization", $"Bearer {token ?? ""}");
        var fullUrl = Client.BuildUri(request).ToString();
        RestResponse res = await Client.ExecuteAsync(request);

        if (res.StatusCode == System.Net.HttpStatusCode.NotFound)
        {

            throw new Exception("404. The endpoint does not exist.");
        }
        else if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new Exception("Not authorized.");
        }


        return (!string.IsNullOrEmpty(res.Content) ? JsonSerializer.Deserialize<ApiResult<T>>(res.Content) : new()) ?? new();
    }
    public async Task<ApiResult> Post(string endpoint, object? body = null)
    {
        if (Client is null) throw new InvalidOperationException("Client is not initialized. Check your endpoint settings first.");
        RestRequest request = new RestRequest(endpoint, Method.Post);

        if (body is not null)
        {
            string jsonString = JsonSerializer.Serialize(body);
            request.AddJsonBody(jsonString);
        }


        string? token = await SecureStorage.GetAsync("access-token");
        if (!string.IsNullOrEmpty(token)) request.AddHeader("Authorization", $"Bearer {token ?? ""}");
        RestResponse res = await Client.ExecuteAsync(request);

        if (res.StatusCode == System.Net.HttpStatusCode.NotFound)
        {

            throw new Exception("404. The endpoint does not exist.");
        }
        else if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new Exception("Not authorized.");
        }


        return (!string.IsNullOrEmpty(res.Content) ? JsonSerializer.Deserialize<ApiResult>(res.Content) : new()) ?? new();
    }
}

