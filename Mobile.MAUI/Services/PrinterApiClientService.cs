using Mobile.MAUI.Helpers;
using Mobile.MAUI.ViewModel;
using RestSharp;
using Shared.Libraries.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mobile.MAUI.Services;

public class PrinterApiClientService
{
    RestClient? Client { get; set; }

    public PrinterApiClientService()
    {

    }

    public void UpdateClient(ClientEndpointVM vm)
    {
        string url = EndpointHelper.BuildPrinterURI(vm);
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

    public async Task<T> GetPrinterApi<T>(string endpoint, object? body = null)
    {
        if (Client is null) throw new InvalidOperationException("Client is not initialized. Check your endpoint settings first.");

        RestRequest request = new RestRequest(endpoint, Method.Get);

        request.AddHeader("Accept", "application/json");

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

        if (string.IsNullOrWhiteSpace(res.Content))
        {
            throw new Exception("Empty response.");
        }

        if (typeof(T) == typeof(string))
        {
            return (T)(object)res.Content.Trim();
        }

        return JsonSerializer.Deserialize<T>(
            res.Content,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })!;
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
