using Mobile.MAUI.Helpers;
using Mobile.MAUI.ViewModel;
using RestSharp;
using Shared.Libraries.Entities;
using System.Text.Json;

namespace Mobile.MAUI.Services;

public class ApiClientService
{
    RestClient? Client { get; set; }

    private Task? _unauthorizedHandling;

    /// <summary>
    /// Raised when the API responds with HTTP 401. Consumers (e.g. the main
    /// layout) use this to clear the local session and route back to login.
    /// </summary>
    public event Func<Task>? Unauthorized;

    public ApiClientService()
    {
        
    }

    private Task NotifyUnauthorizedAsync(string endpoint)
    {
        if (endpoint.Contains("Auth/Login", StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        return _unauthorizedHandling ??= HandleUnauthorizedCoreAsync();

        async Task HandleUnauthorizedCoreAsync()
        {
            try
            {
                var handler = Unauthorized;
                if (handler is not null)
                {
                    await handler.Invoke();
                }
            }
            finally
            {
                _unauthorizedHandling = null;
            }
        }
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
            await NotifyUnauthorizedAsync(endpoint);
            throw new NotAuthorizedException();
        }

        if (!res.IsSuccessStatusCode)
        {
            return ApiResult<T>.Failed(DescribeFailure<T>(res));
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
            await NotifyUnauthorizedAsync(endpoint);
            throw new NotAuthorizedException();
        }

        if (!res.IsSuccessStatusCode)
        {
            return ApiResult<T>.Failed(DescribeFailure<T>(res));
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
            await NotifyUnauthorizedAsync(endpoint);
            throw new NotAuthorizedException();
        }

        if (!res.IsSuccessStatusCode)
        {
            return ApiResult.Failed(DescribeFailure(res));
        }


        return (!string.IsNullOrEmpty(res.Content) ? JsonSerializer.Deserialize<ApiResult>(res.Content) : new()) ?? new();
    }

    /// <summary>
    /// Turns a non-success response into an operator-readable message. A server
    /// that answers with <see cref="ApiResult"/> already carries the reason; an
    /// ASP.NET Core ProblemDetails / HTML body does not, and deserializing it
    /// into <see cref="ApiResult"/> would leave <c>ErrorMessage</c> empty — which
    /// <see cref="ApiResult.Success"/> reads as success, handing a null payload
    /// to OnSuccess and surfacing a LINQ "Value cannot be null. (Parameter
    /// 'source')" instead of the real failure.
    /// </summary>
    private static string DescribeFailure<T>(RestResponse res)
    {
        var message = TryReadApiResult<T>(res)?.ErrorMessage;
        return string.IsNullOrWhiteSpace(message) ? DescribeFailure(res) : message;
    }

    private static string DescribeFailure(RestResponse res)
    {
        var message = TryReadApiResult(res)?.ErrorMessage;
        if (!string.IsNullOrWhiteSpace(message)) return message;

        return $"{(int)res.StatusCode} {res.StatusDescription}".Trim();
    }

    private static ApiResult<T>? TryReadApiResult<T>(RestResponse res)
    {
        try
        {
            return string.IsNullOrEmpty(res.Content) ? null : JsonSerializer.Deserialize<ApiResult<T>>(res.Content);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static ApiResult? TryReadApiResult(RestResponse res)
    {
        try
        {
            return string.IsNullOrEmpty(res.Content) ? null : JsonSerializer.Deserialize<ApiResult>(res.Content);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

