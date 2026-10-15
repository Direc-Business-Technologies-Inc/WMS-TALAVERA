using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Payload;
using Application.DataTransferObjects.Transactions.InventoryCounting.NS;
using Application.DataTransferObjects.Transactions.InventoryCounting.NS.Payload;
using Application.DataTransferObjects.Transactions.Packing.NS;
using Application.DataTransferObjects.Transactions.Packing.NS.Payload;
using Application.DataTransferObjects.Transactions.Receiving.NS;
using Application.DataTransferObjects.Transactions.Receiving.NS.Payload;
using Application.DataTransferObjects.Transactions.TripTicket.NS;
using Application.DataTransferObjects.Transactions.TripTicket.NS.Payload;
using Application.UseCases.Repositories.Integration.Others;
using Database.Libraries.Repositories;
using Integration.NS.Entities;
using Integration.NS.Exceptions;
using Integration.NS.Resilience;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using static Application.DataTransferObjects.Transactions.Commons.NS.ReturnsEnum;

namespace Integration.NS.Services
{
    public class NetSuiteApiClientService(HttpContextAccessor httpContextAccessor, ISqlQueryManager sqlQuery, ILogger<NetSuiteApiClientService> logger) : INetSuiteApiClientService, INotifyPropertyChanged
    {

        string NsDateTimeFormat = "YYYY-MM-DD HH24:MI:SS";
        string DateTimeISOFormat = "YYYY-MM-DD\"T\"HH24:MI:SS";

        private static readonly string AccountId = Environment.GetEnvironmentVariable("ACCOUNT_ID") ?? string.Empty;

        private static readonly string ItemFulfillmentUrl = $"https://{AccountId}.suitetalk.api.netsuite.com/services/rest/record/v1/{{0}}/{{1}}/!transform/itemFulfillment?replace=item.inventoryDetail.inventoryAssignment";
        private static readonly string ItemFulfillmentUrlNotUsedBin = $"https://{AccountId}.suitetalk.api.netsuite.com/services/rest/record/v1/{{0}}/{{1}}/!transform/itemFulfillment?replace=item.inventoryDetail.inventoryAssignment";
        private static readonly string ItemFulfillmentUrlNoReplace = $"https://{AccountId}.suitetalk.api.netsuite.com/services/rest/record/v1/{{0}}/{{1}}/!transform/itemFulfillment";

        private static readonly string PatchItemFulfillmentUrl = $"https://{AccountId}.suitetalk.api.netsuite.com/services/rest/record/v1/itemFulfillment/{{0}}";

        private static readonly string PatchInventoryCountUrl = $"https://{AccountId}.suitetalk.api.netsuite.com/services/rest/record/v1/inventorycount/{{0}}";

        private static readonly string ItemReceiptUrl = $"https://{AccountId}.suitetalk.api.netsuite.com/services/rest/record/v1/{{0}}/{{1}}/!transform/itemReceipt";

        private static readonly string UpdateRecordUrl = $"https://{AccountId}.suitetalk.api.netsuite.com/services/rest/record/v1/{{0}}/{{1}}";

        private static readonly string ClientCredentialsCertificateId = Environment.GetEnvironmentVariable("NETSUITE_CERTIFICATE_ID") ?? string.Empty;

        private static string ItemReceiptRestletUrl = $"https://{AccountId}.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=1853&deploy=1";

        private static string TripTicketRestletUrl = $"https://{AccountId}.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=1862&deploy=1";

        private static string InventoryWorksheetRestletUrl = $"https://{AccountId}.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=1878&deploy=1";

        private static readonly string ApiConsumerKey = Environment.GetEnvironmentVariable("NETSUITE_CONSUMER_KEY") ?? string.Empty;

        private static readonly string PrivateKeyPem = Environment.GetEnvironmentVariable("NETSUITE_PRIVATE_KEY") ?? string.Empty;

        private static readonly string RestApiRoot = $"https://{AccountId}.suitetalk.api.netsuite.com/services/rest";

        private static string Oauth2ApiRoot = $"{RestApiRoot}/auth/oauth2/v1";
        private static string RecordApiRoot = $"{RestApiRoot}/record/v1";

        private static string TokenEndPointUrl = $"{Oauth2ApiRoot}/token";

        private static HttpClient _httpClient = CreateHttpClient();

        /// <summary>
        /// The RSA private key is imported once per process and reused for every
        /// JWT client assertion. Reading/parsing the key file on each token
        /// refresh was wasted work (Req 3.2).
        /// </summary>
        private static readonly Lazy<RSA> _rsaKey = new(LoadPrivateKey, LazyThreadSafetyMode.ExecutionAndPublication);

        private static HttpClient CreateHttpClient()
        {
            return new HttpClient
            {
                // NetSuite's server-side cutoff is 15 minutes; stay well below it
                // so a hung call fails fast instead of pinning a permit (Req 3.3).
                Timeout = TimeSpan.FromSeconds(GetHttpTimeoutSeconds())
            };
        }

        internal static double GetHttpTimeoutSeconds()
        {
            var raw = Environment.GetEnvironmentVariable("NETSUITE_HTTP_TIMEOUT_SECONDS");
            return double.TryParse(raw, out var seconds) && seconds > 0 ? seconds : 100;
        }

        /// <summary>
        /// Maximum simultaneous NetSuite requests per process. Defaults to 2 so
        /// two hosts cannot exceed the account concurrency limit of 5; an
        /// explicit environment value is honoured with a floor of 1.
        /// </summary>
        internal static int GetMaxInFlightPerProcess()
        {
            var raw = Environment.GetEnvironmentVariable("NETSUITE_MAX_INFLIGHT_PER_PROCESS");
            return int.TryParse(raw, out var value) ? Math.Max(1, value) : 2;
        }

        /// <summary>
        /// Test seam: replaces the shared <see cref="HttpClient"/>. Production
        /// behaviour is untouched when never called.
        /// </summary>
        internal static void OverrideHttpClientForTests(HttpClient client)
        {
            _httpClient = client;
        }

        /// <summary>
        /// Test seam: points request roots at a fake (e.g. WireMock). Only the
        /// roots tests need are overridable; <see cref="ResetUrlRootsForTests"/>
        /// restores production values.
        /// </summary>
        internal static void OverrideUrlRootsForTests(
            string? suiteQlRoot = null,
            string? recordApiRoot = null,
            string? itemReceiptRestlet = null,
            string? tripTicketRestlet = null,
            string? inventoryWorksheetRestlet = null)
        {
            if (suiteQlRoot is not null) SuiteQLRoot = suiteQlRoot;
            if (recordApiRoot is not null) RecordApiRoot = recordApiRoot;
            if (itemReceiptRestlet is not null) ItemReceiptRestletUrl = itemReceiptRestlet;
            if (tripTicketRestlet is not null) TripTicketRestletUrl = tripTicketRestlet;
            if (inventoryWorksheetRestlet is not null) InventoryWorksheetRestletUrl = inventoryWorksheetRestlet;
        }

        internal static void ResetUrlRootsForTests()
        {
            SuiteQLRoot = $"{RestApiRoot}/query/v1/suiteql";
            RecordApiRoot = $"{RestApiRoot}/record/v1";
            ItemReceiptRestletUrl = $"https://{AccountId}.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=1853&deploy=1";
            TripTicketRestletUrl = $"https://{AccountId}.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=1862&deploy=1";
            InventoryWorksheetRestletUrl = $"https://{AccountId}.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=1878&deploy=1";
        }

        /// <summary>
        /// Test seam: rebuilds the shared HttpClient at production defaults so
        /// suites that override it (WireMock-created clients, short-timeout
        /// stubs) cannot leak their stubs into order-dependent tests that assert
        /// on the shared client's configuration.
        /// </summary>
        internal static void ResetHttpClientForTests()
        {
            _httpClient = CreateHttpClient();
        }

        private static RSA LoadPrivateKey()
        {
            string privateKeyPem = File.ReadAllText(Environment.GetEnvironmentVariable("PRIVATEKEY_PATH") ?? string.Empty);
            // Remove headers and footers
            privateKeyPem = privateKeyPem.Replace("-----BEGIN PRIVATE KEY-----", "")
                                         .Replace("-----END PRIVATE KEY-----", "")
                                         .Replace("\n", "")
                                         .Replace("\r", "");
            // Decode the Base64 key
            byte[] privateKeyRaw = Convert.FromBase64String(privateKeyPem);
            var rsa = RSACng.Create();
            rsa.KeySize = 3072;
            rsa.ImportPkcs8PrivateKey(privateKeyRaw, out _);
            // Ensure the key size is at least 3072 bits
            if (rsa.KeySize < 3072)
            {
                throw new Exception("RSA key size must be at least 3072 bits for NetSuite OAuth.");
            }

            return rsa;
        }

        private static string _accessToken;
        private static DateTime _tokenExpiryTime;

        private static string SuiteQLRoot = $"{RestApiRoot}/query/v1/suiteql";


        private static DateTime _jwtTokenExpiryTime = DateTime.MinValue;
        private string tokenText = "";

        public event PropertyChangedEventHandler? PropertyChanged;
        JsonSerializerOptions JsonSerializerOption = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        };

        JsonSerializerOptions JsonSerializerRequestOption = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
        };
        int _alreadySyncedItems = 0;
        public int AlreadySyncedItems
        {
            get => _alreadySyncedItems; set
            {
                if (_alreadySyncedItems != value)
                {
                    _alreadySyncedItems = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(_alreadySyncedItems)));
                }
            }
        }
        int _totalItemsToSync = 0;
        public int TotalItemsToSync
        {
            get => _totalItemsToSync; set
            {
                if (_totalItemsToSync != value)
                {
                    _totalItemsToSync = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(_totalItemsToSync)));
                }
            }
        }

        public async Task<string> GetAccessToken(CancellationToken ct = default)
        {
            return await GetAccessToken((request, cancellationToken) =>
                _httpClient.SendAsync(request, cancellationToken), ct);
        }

        /// <summary>
        /// Token acquisition with an injectable sender so the refresh flow can be
        /// exercised in unit tests. This method intentionally does NOT set
        /// <c>_tokenExpiryTime</c>: expiry is owned exclusively by
        /// <see cref="GetValidAccessToken"/> after a successful refresh (Req 3.1).
        /// </summary>
        internal async Task<string> GetAccessToken(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync,
            CancellationToken ct = default)
        {
            var url = Oauth2ApiRoot + "/token";
            string clientAssertion = GetJwtToken();

            var requestParams = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"),
                new KeyValuePair<string, string>("client_assertion", clientAssertion)
            };

            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, url);

            httpRequest.Content = new FormUrlEncodedContent(requestParams);

            var httpResponse = await sendAsync(httpRequest, ct);
            string res = await httpResponse.Content.ReadAsStringAsync(ct);
            if (httpResponse.IsSuccessStatusCode)
            {
                var responseJson = await httpResponse.Content.ReadAsStringAsync();
                var response = System.Text.Json.JsonSerializer.Deserialize<NetSuiteToken>(responseJson);
                return response?.access_token ?? throw new Exception("Access token not found in response.");
            }
            else
            {
                var errorContent = await httpResponse.Content.ReadAsStringAsync();
                // Log the full error response
                throw new Exception($"Error retrieving access token: {httpResponse.StatusCode} - {errorContent}");
            }
        }

        private string GetJwtToken()
        {

            if (!string.IsNullOrEmpty(tokenText) && DateTime.Now < _jwtTokenExpiryTime)
            {
                return tokenText; // Return the existing token if it's still valid.
            }
            // Use the cached RSA key (imported once per process).
            var rsa = _rsaKey.Value;
            // Use RSA-PSS (PS256) instead of RS256
            var rsaSecurityKey = new RsaSecurityKey(rsa)
            {
                KeyId = ClientCredentialsCertificateId
            };
            var signingCreds = new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSsaPssSha256);
            var now = DateTime.UtcNow;
            var tokenHandler = new JwtSecurityTokenHandler { SetDefaultTimesOnTokenCreation = false };
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = ApiConsumerKey,
                Audience = TokenEndPointUrl,
                Expires = now.AddMinutes(10),
                IssuedAt = now,
                Claims = new Dictionary<string, object>
            {
                { "scope", "restlets rest_webservices" }
            },
                SigningCredentials = signingCreds
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        string FormatQuery(string query)
        {
            return Regex.Replace(query, @"\s+", " ").Trim();
        }

        private static readonly SemaphoreSlim _tokenSemaphore = new(1, 1);
        private static readonly SemaphoreSlim _requestSemaphore =
            new(GetMaxInFlightPerProcess(), GetMaxInFlightPerProcess());

        //public async Task<T> MakeRequest<T>(string url, string? reqBody, HttpMethod method)
        //{
        //    if (_accessToken == null || DateTime.Now >= _tokenExpiryTime)
        //        _accessToken = await GetAccessToken();

        //    using var httpRequest = new HttpRequestMessage(method, url);

        //    httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        //    // Add other custom headers
        //    httpRequest.Headers.Add("Prefer", "transient");

        //    if (!string.IsNullOrEmpty(reqBody))
        //    {
        //        //_logger.LogDebug("SuiteQLQuery Request: {@Request}", reqBody);
        //        httpRequest.Content = new StringContent(reqBody, Encoding.UTF8, "application/json");
        //    }

        //    var httpResponse = await _httpClient.SendAsync(httpRequest);

        //    var responseJson = await httpResponse.Content.ReadAsStringAsync();

        //    if (httpResponse.IsSuccessStatusCode)
        //    {
        //        //_logger.LogDebug("SuiteQLQuery Result: {@Result}", responseJson);
        //        if (string.IsNullOrEmpty(responseJson))
        //        {
        //            return default(T);
        //        }

        //        var response = JsonSerializer.Deserialize<T>(responseJson, JsonSerializerRequestOption);
        //        if (response == null) throw new Exception("Bad response from NetSuite API");
        //        return response;
        //    }
        //    var errorBody = await httpResponse.Content.ReadFromJsonAsync<NetSuiteErrorResponse>();
        //    throw new Exception(errorBody?.DisplayString ?? $"Request failed with status code: {httpResponse.StatusCode}");
        //}

        public async Task<T> MakeRequest<T>(
        string url,
        string? reqBody,
        HttpMethod method,
        NetSuiteRequestKind? kind = null,
        NetSuiteRetryProfile profile = NetSuiteRetryProfile.Interactive,
        CancellationToken ct = default)
        {
            var accessToken = await GetValidAccessToken(ct);

            var requestKind = kind ?? NetSuiteRequestClassifier.Classify(url, method);
            var pipeline = NetSuitePipelines.For(profile, requestKind);

            // Limit concurrent calls to NetSuite. The retry pipeline executes
            // inside the permit hold (D5) so a 429 storm cannot starve the
            // limiter by re-acquiring between attempts.
            await _requestSemaphore.WaitAsync(ct);

            try
            {
                return await pipeline.ExecuteAsync(
                    pipelineCt => SendAndHandleOAuth2<T>(url, reqBody, method, accessToken, pipelineCt),
                    ct);
            }
            finally
            {
                _requestSemaphore.Release();
            }
        }

        private async ValueTask<T> SendAndHandleOAuth2<T>(
            string url,
            string? reqBody,
            HttpMethod method,
            string accessToken,
            CancellationToken ct = default)
        {
            using var httpRequest =
                new HttpRequestMessage(method, url);

            httpRequest.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken);

            httpRequest.Headers.Add(
                "Prefer",
                "transient");

            if (!string.IsNullOrEmpty(reqBody))
            {
                httpRequest.Content = new StringContent(
                    reqBody,
                    Encoding.UTF8,
                    "application/json");
            }

            using var httpResponse =
                await SendRequestAsync(httpRequest, ct);

            var responseJson =
                await httpResponse.Content.ReadAsStringAsync(ct);

            if (httpResponse.IsSuccessStatusCode)
            {
                if (string.IsNullOrEmpty(responseJson))
                    return default!;

                var response =
                    JsonSerializer.Deserialize<T>(
                        responseJson,
                        JsonSerializerRequestOption);

                return response
                    ?? throw new Exception(
                        "Bad response from NetSuite API");
            }

            // Parse the error from the string already read above; a second
            // ReadFromJsonAsync content read can throw JsonException and mask
            // the real status code (G2/G3).
            string? displayString = null;

            try
            {
                displayString =
                    JsonSerializer.Deserialize<NetSuiteErrorResponse>(
                        responseJson,
                        JsonSerializerRequestOption)?.DisplayString;
            }
            catch (JsonException)
            {
                // Unrecognized body — fall back to the detail chain below.
            }

            var detail = !string.IsNullOrWhiteSpace(displayString)
                ? displayString
                : GetNetSuiteErrorDetail(responseJson);

            LogNonSuccessResponse(httpResponse, method, url, reqBody, responseJson);

            throw NetSuiteExceptionFactory.CreateForStatus(
                httpResponse.StatusCode,
                BuildNetSuiteErrorMessage(
                    httpResponse,
                    detail,
                    responseJson),
                detail,
                reqBody);
        }

        /// <summary>
        /// Sends a request, mapping transport/network failures to
        /// <see cref="NetSuiteTransientException"/> so the retry policy can treat
        /// them uniformly with 429/5xx responses (Req 4.1).
        /// Cancellation requested via <paramref name="ct"/> is never mapped or
        /// retried: it propagates as <see cref="OperationCanceledException"/>
        /// so Blazor navigation-away simply stops updating (Req 3.1, 3.3).
        /// </summary>
        private static async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request, CancellationToken ct = default)
        {
            try
            {
                return await _httpClient.SendAsync(request, ct);
            }
            catch (OperationCanceledException ex) when (ct.IsCancellationRequested)
            {
                throw new OperationCanceledException("NetSuite request was cancelled.", ex, ct);
            }
            catch (HttpRequestException ex)
            {
                throw NetSuiteExceptionFactory.CreateForNetworkError(
                    $"NetSuite request failed: {ex.Message}",
                    ex);
            }
            catch (TaskCanceledException ex)
            {
                throw NetSuiteExceptionFactory.CreateForNetworkError(
                    $"NetSuite request timed out: {ex.Message}",
                    ex);
            }
        }

        private async Task<string> GetValidAccessToken(CancellationToken ct = default)
        {
            // Fast path — most requests should come through here.
            if (_accessToken != null &&
                DateTime.UtcNow < _tokenExpiryTime)
            {
                return _accessToken;
            }

            // Only one request can refresh the token at a time.
            await _tokenSemaphore.WaitAsync(ct);

            try
            {
                // Double-check!
                //
                // Another request may have refreshed the token
                // while this request was waiting for the semaphore.
                if (_accessToken != null &&
                    DateTime.UtcNow < _tokenExpiryTime)
                {
                    return _accessToken;
                }

                var newToken = await GetAccessToken(ct);

                _accessToken = newToken;

                // NetSuite token is valid for 10 minutes.
                // Refresh 1 minute early.
                _tokenExpiryTime =
                    DateTime.UtcNow.AddMinutes(10);

                return _accessToken;
            }
            finally
            {
                _tokenSemaphore.Release();
            }
        }


        async Task<T> MakeRequest<T>(string url, string? reqBody = null, CancellationToken ct = default)
        {
            return await MakeRequest<T>(url, reqBody, HttpMethod.Post, ct: ct);
        }

        public async Task<T> MakeRequestOAuth1<T>(
            string url,
            string? reqBody,
            HttpMethod method,
            NetSuiteRequestKind? kind = null,
            NetSuiteRetryProfile profile = NetSuiteRetryProfile.Interactive,
            CancellationToken ct = default)
        {
            string consumerKey = Environment.GetEnvironmentVariable("OAUTH1_CONSUMER_KEY") ?? "";
            string consumerSecret = Environment.GetEnvironmentVariable("OAUTH1_CONSUMER_SECRET") ?? "";
            string token = Environment.GetEnvironmentVariable("OAUTH1_TOKEN_ID") ?? "";
            string tokenSecret = Environment.GetEnvironmentVariable("OAUTH1_TOKEN_SECRET") ?? "";

            string nonce = Guid.NewGuid().ToString("N");
            string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

            var uri = new Uri(url);

            // Base URL WITHOUT query string
            string baseUrl = $"https://{AccountId}.restlets.api.netsuite.com/app/site/hosting/restlet.nl";

            // Parse query string parameters
            var parameters = new SortedDictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(uri.Query))
            {
                var query = uri.Query.TrimStart('?');

                foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    var parts = pair.Split('=', 2);

                    var key = Uri.UnescapeDataString(parts[0]);
                    var value = parts.Length > 1
                        ? Uri.UnescapeDataString(parts[1])
                        : "";

                    parameters[key] = value;
                }
            }

            // OAuth parameters
            parameters["oauth_consumer_key"] = consumerKey;
            parameters["oauth_nonce"] = nonce;
            parameters["oauth_signature_method"] = "HMAC-SHA256";
            parameters["oauth_timestamp"] = timestamp;
            parameters["oauth_token"] = token;
            parameters["oauth_version"] = "1.0";

            // Normalize parameters
            string normalizedParameters = string.Join("&",
                parameters.Select(p =>
                    $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

            string httpMethod = method.Method.ToUpperInvariant();

            // Signature base string
            string signatureBaseString =
                $"{httpMethod}&{Uri.EscapeDataString(baseUrl)}&{Uri.EscapeDataString(normalizedParameters)}";

            // Signing key
            string signingKey =
                $"{Uri.EscapeDataString(consumerSecret)}&{Uri.EscapeDataString(tokenSecret)}";

            string signature;

            using (var hmac = new System.Security.Cryptography.HMACSHA256(
                Encoding.UTF8.GetBytes(signingKey)))
            {
                signature = Convert.ToBase64String(
                    hmac.ComputeHash(
                        Encoding.UTF8.GetBytes(signatureBaseString)));
            }

            string authorizationHeader =
                "OAuth " +
                $"realm=\"{AccountId}\", " +
                $"oauth_consumer_key=\"{Uri.EscapeDataString(consumerKey)}\", " +
                $"oauth_token=\"{Uri.EscapeDataString(token)}\", " +
                $"oauth_signature_method=\"HMAC-SHA256\", " +
                $"oauth_timestamp=\"{timestamp}\", " +
                $"oauth_nonce=\"{nonce}\", " +
                $"oauth_version=\"1.0\", " +
                $"oauth_signature=\"{Uri.EscapeDataString(signature)}\"";

            var requestKind = kind ?? NetSuiteRequestClassifier.Classify(url, method);
            var pipeline = NetSuitePipelines.For(profile, requestKind);

            // Defense-in-depth: the OAuth1 path shares the same limiter as the
            // OAuth2 path (two separate semaphores would sum back over budget).
            await _requestSemaphore.WaitAsync(ct);

            try
            {
                return await pipeline.ExecuteAsync(
                    pipelineCt => SendAndHandleOAuth1<T>(url, reqBody, method, authorizationHeader, pipelineCt),
                    ct);
            }
            finally
            {
                _requestSemaphore.Release();
            }
        }

        private async ValueTask<T> SendAndHandleOAuth1<T>(
            string url,
            string? reqBody,
            HttpMethod method,
            string authorizationHeader,
            CancellationToken ct = default)
        {
            using var request = new HttpRequestMessage(method, url);

            request.Headers.TryAddWithoutValidation(
                "Authorization",
                authorizationHeader);

            if (!string.IsNullOrEmpty(reqBody))
            {
                request.Content = new StringContent(
                    reqBody,
                    Encoding.UTF8,
                    "application/json");
            }

            using var httpResponse = await SendRequestAsync(request, ct);

            var responseJson = await httpResponse.Content.ReadAsStringAsync(ct);
            if (httpResponse.IsSuccessStatusCode)
            {
                if (string.IsNullOrEmpty(responseJson))
                    return default!;

                return System.Text.Json.JsonSerializer.Deserialize<T>(responseJson);
            }

            var errorDetail = GetNetSuiteErrorDetail(responseJson);

            LogNonSuccessResponse(httpResponse, method, url, reqBody, responseJson);

            throw NetSuiteExceptionFactory.CreateForStatus(
                httpResponse.StatusCode,
                BuildNetSuiteErrorMessage(
                    httpResponse,
                    errorDetail,
                    responseJson),
                errorDetail,
                reqBody);
        }

        private static string? GetNetSuiteErrorDetail(string responseJson)
        {
            if (string.IsNullOrWhiteSpace(responseJson))
                return null;

            try
            {
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                // Format 1:
                // {
                //   "o:errorDetails": [
                //     {
                //       "detail": "Error while accessing a resource..."
                //     }
                //   ]
                // }
                if (root.TryGetProperty("o:errorDetails", out var errorDetails) &&
                    errorDetails.ValueKind == JsonValueKind.Array &&
                    errorDetails.GetArrayLength() > 0)
                {
                    var firstError = errorDetails[0];

                    if (firstError.TryGetProperty("detail", out var detailElement) &&
                        detailElement.ValueKind == JsonValueKind.String)
                    {
                        var detail = detailElement.GetString();

                        if (!string.IsNullOrWhiteSpace(detail))
                            return detail;
                    }
                }

                // Format 2:
                // {
                //   "success": false,
                //   "error": "The field estamount contained..."
                // }
                // Format 3 (RESTlet object shape):
                // {
                //   "success": false,
                //   "error": { "code": "...", "message": "...", "title": "..." }
                // }
                if (root.TryGetProperty("error", out var errorElement))
                {
                    if (errorElement.ValueKind == JsonValueKind.String)
                    {
                        var error = errorElement.GetString();

                        if (!string.IsNullOrWhiteSpace(error))
                            return error;
                    }
                    else if (errorElement.ValueKind == JsonValueKind.Object)
                    {
                        if (TryReadStringProperty(errorElement, "message", out var objectMessage))
                            return objectMessage;

                        if (TryReadStringProperty(errorElement, "title", out var objectTitle))
                            return objectTitle;

                        if (TryReadStringProperty(errorElement, "code", out var objectCode))
                            return objectCode;
                    }
                }

                // Final fallbacks: top-level title / message.
                if (TryReadStringProperty(root, "title", out var title))
                    return title;

                if (TryReadStringProperty(root, "message", out var message))
                    return message;

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static bool TryReadStringProperty(
            JsonElement element,
            string name,
            out string value)
        {
            value = string.Empty;

            if (element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty(name, out var property) &&
                property.ValueKind == JsonValueKind.String)
            {
                var text = property.GetString();

                if (!string.IsNullOrWhiteSpace(text))
                {
                    value = text;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Builds a non-null NetSuite failure message of the form
        /// "NetSuite &lt;code&gt; &lt;reason&gt;: &lt;detail&gt;", where the detail is the
        /// parsed error when available, otherwise a raw-body excerpt of at most
        /// 2000 characters.
        /// </summary>
        internal static string BuildNetSuiteErrorMessage(
            HttpResponseMessage response,
            string? detail,
            string responseBody)
        {
            var statusLine =
                $"NetSuite {(int)response.StatusCode} " +
                $"{response.ReasonPhrase ?? response.StatusCode.ToString()}";

            // A parsed detail shorter than 10 characters (e.g. "(0)") carries
            // no actionable information; append the raw-body excerpt so the
            // real NetSuite error stays visible without log access.
            if (!string.IsNullOrWhiteSpace(detail) && detail.Length >= 10)
                return $"{statusLine}: {detail}";

            var bodyExcerpt = string.Empty;

            if (!string.IsNullOrWhiteSpace(responseBody))
            {
                const int maxBodyExcerpt = 2000;

                bodyExcerpt = responseBody.Length <= maxBodyExcerpt
                    ? responseBody
                    : responseBody[..maxBodyExcerpt];
            }

            if (!string.IsNullOrWhiteSpace(detail))
                return $"{statusLine}: {detail} | raw body: {bodyExcerpt}";

            if (string.IsNullOrWhiteSpace(responseBody))
                return $"{statusLine}: empty response body";

            return $"{statusLine}: {bodyExcerpt}";
        }

        /// <summary>
        /// Warning-level record of every failed NetSuite request: URL, status,
        /// request payload (so the fulfillment id actually sent is visible) and
        /// response body. Authorization headers and tokens are never logged.
        /// </summary>
        private void LogNonSuccessResponse(
            HttpResponseMessage response,
            HttpMethod method,
            string url,
            string? requestBody,
            string responseBody)
        {
            logger.LogWarning(
                "NetSuite request failed: {Method} {Url} returned {StatusCode} {ReasonPhrase}. Request payload: {RequestBody} Response body: {ResponseBody}",
                method.Method,
                url,
                (int)response.StatusCode,
                response.ReasonPhrase,
                requestBody,
                responseBody);
        }

        public async Task<IEnumerable<T>?> NetsuiteQuery<T>(
            string queryName,
            Dictionary<string, string>? parameters = null,
            int limit = 0,
            int offset = 0,
            CancellationToken ct = default)
        {
            var url = SuiteQLRoot;

            if (limit > 0 && offset >= 0)
            {
                url += $"?limit={limit}&offset={offset}";
            }

            sqlQuery.GetSqlScriptWithMetadata(queryName, out string query, out bool isFound);

            if (!isFound)
            {
                throw new Exception($"SQL query '{queryName}' not found.");
            }

            // Parameter substitution rules (including the {key} raw-splice guard)
            // live in SuiteQLText — shared with SuiteQLScriptResolver.
            query = Helpers.SuiteQLText.SubstituteScriptParameters(query, parameters);

            var jsonBody = System.Text.Json.JsonSerializer.Serialize(new
            {
                q = FormatQuery(query)
            });

            var result = await MakeRequest<NetSuiteResponse<T>>(url, jsonBody, HttpMethod.Post, ct: ct);

            return result.items;
        }

        public async Task<NetSuiteResponse<T>> ExecuteSuiteQLQuery<T>(string query, int? limit = null, int? offset = null, CancellationToken ct = default)
        {
            var url = SuiteQLRoot;
            if (limit.HasValue) url += $"?limit={limit.Value}" + (offset.HasValue ? $"&offset={offset.Value}" : "");
            else if (offset.HasValue) url += $"?offset={offset.Value}";

            query = FormatQuery(query);

            var jsonBody = JsonSerializer.Serialize(new { q = query });

            return await MakeRequest<NetSuiteResponse<T>>(url, jsonBody, HttpMethod.Post, ct: ct);
        }

        #region Receiving
        public async Task<bool> SavePOItemReceipt(List<PostPurchaseOrderDTO> Data, int userId, string remarks)
        {

            var orderId = Data.Select(x => x.NetsuiteOrderInternalId).FirstOrDefault();
            string url = string.Format(ItemReceiptUrl, "purchaseOrder", orderId);

            var badPO = Data.Where(x => x.IsBad && x.ScannedQuantity > 0).ToList();

            if (badPO.Any())
            {
                try
                {
                    var payloadBad = PurchaseOrderIRPayloadDTO.CreateForItemReceipt(Data, userId, 2, remarks);

                    var jsonStringBad = JsonSerializer.Serialize(payloadBad, JsonSerializerOption);

                    await MakeRequest<object>(url, jsonStringBad, HttpMethod.Post);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error while posting BAD PO Item Receipt. {ex.Message}", ex);
                }
            }

            var goodPO = Data.Where(x => !x.IsBad && x.ScannedQuantity > 0).ToList();

            if (goodPO.Any())
            {
                try
                {
                    var payloadGood = PurchaseOrderIRPayloadDTO.CreateForItemReceipt(Data, userId, 1, remarks);

                    var jsonStringGood = JsonSerializer.Serialize(payloadGood, JsonSerializerOption);

                    await MakeRequest<object>(url, jsonStringGood, HttpMethod.Post);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error while posting GOOD PO Item Receipt. {ex.Message}", ex);
                }
            }

            return true;
        }

        public async Task<bool> SaveTOItemReceipt(List<PostTransferOrderDTO> Data, int TONetsuiteOrderInternalId, int userId, string remarks)
        {
            var IFOrderId = Data.Select(x => x.NetsuiteOrderInternalId).FirstOrDefault();

            string url = ItemReceiptRestletUrl;

            var badTO = Data.Where(x => x.IsBad && !x.IsMissing).ToList();

            if (badTO.Any(x => x.ScannedQuantity > 0))
            {
                try
                {
                    var payloadBad = TransferOrderIRRestletPayloadDTO.CreateForItemReceiptRestlet(badTO, TONetsuiteOrderInternalId, IFOrderId, userId, 3, remarks);

                    var jsonStringBad = JsonSerializer.Serialize(payloadBad, JsonSerializerOption);

                    await MakeRequest<object>(url, jsonStringBad, HttpMethod.Post);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error while posting BAD TO Item Receipt. {ex.Message}", ex);
                }
            }

            var goodTO = Data.Where(x => !x.IsBad && !x.IsMissing).ToList();

            if (goodTO.Any())
            {
                try
                {
                    var payloadGood = TransferOrderIRRestletPayloadDTO.CreateForItemReceiptRestlet(goodTO, TONetsuiteOrderInternalId, IFOrderId, userId, 1, remarks);

                    var jsonStringGood = JsonSerializer.Serialize(payloadGood, JsonSerializerOption);

                    await MakeRequest<object>(url, jsonStringGood, HttpMethod.Post);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error while posting GOOD TO Item Receipt. {ex.Message}", ex);
                }
            }

            var missingTO = Data.Where(x => x.IsMissing).ToList();
            if (missingTO.Any(x => x.ScannedQuantity > 0))
            {
                try
                {
                    var payloadBad = TransferOrderIRRestletPayloadDTO.CreateForItemReceiptRestlet(missingTO, TONetsuiteOrderInternalId, IFOrderId, userId, 5, remarks);

                    var jsonStringBad = JsonSerializer.Serialize(payloadBad, JsonSerializerOption);

                    await MakeRequest<object>(url, jsonStringBad, HttpMethod.Post);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error while posting Missing TO Item Receipt. {ex.Message}", ex);
                }
            }

            return true;
        }

        public async Task<bool> SaveReturnsItemReceipt(List<PostReturnsDTO> Data, int TONetsuiteOrderInternalId, int userId, string remarks)
        {
            var orderId = Data.Select(x => x.NetsuiteOrderInternalId).FirstOrDefault();
            string url = string.Format(ItemReceiptUrl + "?replace=item.inventoryDetail.inventoryAssignment", "transferOrder", TONetsuiteOrderInternalId);
            //string url = string.Format(ItemReceiptUrl, "transferOrder", TONetsuiteOrderInternalId);

            var transferCategory = (TransferCategory)Data.Select(x => x.TransferCategory).FirstOrDefault();

            var receivingCategory = transferCategory == TransferCategory.GoodItems ? 1 : 2;

            var returns = Data.Where(x => !x.IsMissing).ToList();
            if (returns.Any(x => x.ScannedQuantity > 0))
            {
                try
                {
                    ReturnsIRPayloadDTO payloadGood = ReturnsIRPayloadDTO.CreateForItemReceipt(returns, receivingCategory, orderId, userId, remarks);

                    var jsonStringGood = JsonSerializer.Serialize(payloadGood, JsonSerializerOption);

                    await MakeRequest<object>(url, jsonStringGood, HttpMethod.Post);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error while posting BAD Returns Item Receipt. {ex.Message}", ex);
                }
            }

            var missingReturns = Data.Where(x => x.IsMissing).ToList();
            if (missingReturns.Any(x => x.ScannedQuantity > 0))
            {
                try
                {
                    ReturnsIRPayloadDTO payloadGood = ReturnsIRPayloadDTO.CreateForItemReceipt(missingReturns, 5, orderId, userId, remarks);

                    var jsonStringGood = JsonSerializer.Serialize(payloadGood, JsonSerializerOption);

                    await MakeRequest<object>(url, jsonStringGood, HttpMethod.Post);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error while posting Missing TO Item Receipt. {ex.Message}", ex);
                }
            }

            return true;
        }
        #endregion

        #region Itemfulfillment
        public async Task<bool> SaveTOItemFulfillment(List<PostTransferOrderDTO> Data)
        {
            var orderId = Data.Select(x => x.NetsuiteOrderInternalId).FirstOrDefault();
            var isUsedBin = Data.Select(x => x.IsLocationUsedBin).FirstOrDefault();
            var isTripTicketExempt = Data.FirstOrDefault()?.IsTripTicketExempt == true;
            var itemFulfillmentStatus = isTripTicketExempt ? "C" : "B";
            var printLabel = true; // Packing labels are required regardless of trip-ticket exemption.

            var badTO = Data.Where(x => x.IsBad).ToList();

            if (badTO.Any(x => x.ScannedQuantity > 0))
            {
                // replace + explicit inventoryDetail only for bin-tracked locations; non-bin posts run the legacy shape (no detail, no replace).
                var url = string.Format(isUsedBin ? ItemFulfillmentUrl : ItemFulfillmentUrlNoReplace, "transferOrder", orderId);

                string? jsonStringBad = null;
                try
                {
                    var payloadBad = TransferOrderIFPayloadDTO.CreateForItemFulfillment(badTO, itemFulfillmentStatus, printLabel, isUsedBin);

                    jsonStringBad = JsonSerializer.Serialize(payloadBad, JsonSerializerOption);

                    await MakeRequest<object>(url, jsonStringBad, HttpMethod.Post);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "BAD TO Item Fulfillment failed. Url: {Url}. Payload: {Payload}", url, jsonStringBad);
                    var diag = $"[diag replace={url.Contains("replace=")} usedBin={isUsedBin} scannedLines={badTO.Count(x => x.ScannedQuantity > 0)} totalLines={badTO.Count}]";
                    throw new Exception($"Error while posting BAD TO Item Fulfillment. {ex.Message} {diag}", ex);
                }
            }

            var goodTO = Data.Where(x => !x.IsBad).ToList();

            if (goodTO.Any())
            {
                // replace + explicit inventoryDetail only when the location is bin-tracked AND the bucket carries scanned lines.
                var usedBinWithScan = isUsedBin && goodTO.Any(x => x.ScannedQuantity > 0);
                var url = string.Format(usedBinWithScan ? ItemFulfillmentUrl : ItemFulfillmentUrlNoReplace, "transferOrder", orderId);

                string? jsonStringGood = null;
                try
                {
                    var payloadGood = TransferOrderIFPayloadDTO.CreateForItemFulfillment(goodTO, itemFulfillmentStatus, printLabel, isUsedBin);

                    jsonStringGood = JsonSerializer.Serialize(payloadGood, JsonSerializerOption);

                    await MakeRequest<object>(url, jsonStringGood, HttpMethod.Post);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "GOOD TO Item Fulfillment failed. Url: {Url}. Payload: {Payload}", url, jsonStringGood);
                    var diag = $"[diag replace={url.Contains("replace=")} usedBin={isUsedBin} scannedLines={goodTO.Count(x => x.ScannedQuantity > 0)} totalLines={goodTO.Count}]";
                    throw new Exception($"Error while posting GOOD TO Item Fulfillment. {ex.Message} {diag}", ex);
                }
            }
            return true;
        }

        public async Task<bool> SaveReturnsItemFulfillment(List<PostReturnsDTO> Data)
        {
            var orderId = Data.Select(x => x.NetsuiteOrderInternalId).FirstOrDefault();
            var isUsedBin = Data.Select(x => x.IsLocationUsedBin).FirstOrDefault();
            var isTripTicketExempt = Data.FirstOrDefault()?.IsTripTicketExempt == true;
            var itemFulfillmentStatus = isTripTicketExempt ? "C" : "B";
            var printLabel = true; // Packing labels are required regardless of trip-ticket exemption.

            // replace + explicit inventoryDetail only when the location is bin-tracked AND the payload carries scanned lines; otherwise legacy shape.
            var returnsUsedBinWithScan = isUsedBin && Data.Any(x => x.ScannedQuantity > 0);
            string url = string.Format(returnsUsedBinWithScan ? ItemFulfillmentUrl : ItemFulfillmentUrlNoReplace, "transferOrder", orderId);

            string? jsonStringGood = null;
            try
            {
                ReturnsIFPayloadDTO payloadGood = ReturnsIFPayloadDTO.CreateForItemFulfillment(Data, itemFulfillmentStatus, printLabel, isUsedBin);

                jsonStringGood = JsonSerializer.Serialize(payloadGood, JsonSerializerOption);

                await MakeRequest<object>(url, jsonStringGood, HttpMethod.Post);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Returns Item Fulfillment failed. Url: {Url}. Payload: {Payload}", url, jsonStringGood);
                var diag = $"[diag replace={url.Contains("replace=")} usedBin={isUsedBin} scannedLines={Data.Count(x => x.ScannedQuantity > 0)} totalLines={Data.Count}]";
                throw new Exception($"Error while posting Returns Item Fulfillment. {ex.Message} {diag}", ex);
            }

            return true;
        }

        public async Task<bool> SaveVRAItemFulfillment(List<PostVendorReturnAuthorizationDTO> Data)
        {
            var orderId = Data.Select(x => x.NetsuiteOrderInternalId).FirstOrDefault();
            var isUsedBin = Data.Select(x => x.IsLocationUsedBin).FirstOrDefault();
            var isTripTicketExempt = Data.FirstOrDefault()?.IsTripTicketExempt == true;
            var itemFulfillmentStatus = isTripTicketExempt ? "C" : "B";
            var printLabel = true; // Packing labels are required regardless of trip-ticket exemption.

            //string url = string.Format(isUsedBin ? ItemFulfillmentUrl : ItemFulfillmentUrlNotUsedBin, "vendorReturnAuthorization", orderId);
            // Drop ?replace= per bucket when that bucket carries no inventoryDetail, otherwise NetSuite rejects the transform with INVALID_PARAMETER.

            var badTO = Data.Where(x => x.IsBad).ToList();

            if (badTO.Any(x => x.ScannedQuantity > 0))
            {
                // Every posted bad line has scanned qty; replace + detail only for bin-tracked locations.
                var urlBad = string.Format(isUsedBin ? ItemFulfillmentUrl : ItemFulfillmentUrlNoReplace, "vendorReturnAuthorization", orderId);

                string? jsonStringBad = null;
                try
                {
                    var payloadBad = VendorReturnAuthorizationIFPayloadDTO.CreateForItemFulfillment(badTO, itemFulfillmentStatus, printLabel, isUsedBin);

                    jsonStringBad = JsonSerializer.Serialize(payloadBad, JsonSerializerOption);

                    await MakeRequest<object>(urlBad, jsonStringBad, HttpMethod.Post);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "BAD VRA Item Fulfillment failed. Url: {Url}. Payload: {Payload}", urlBad, jsonStringBad);
                    var diag = $"[diag replace={urlBad.Contains("replace=")} usedBin={isUsedBin} scannedLines={badTO.Count(x => x.ScannedQuantity > 0)} totalLines={badTO.Count}]";
                    throw new Exception($"Error while posting BAD VRA Item Fulfillment. {ex.Message} {diag}", ex);
                }
            }

            var goodTO = Data.Where(x => !x.IsBad).ToList();

            if (goodTO.Any())
            {
                // replace + explicit inventoryDetail only when the location is bin-tracked AND the bucket carries scanned lines.
                var goodUsedBinWithScan = isUsedBin && goodTO.Any(x => x.ScannedQuantity > 0);
                var urlGood = string.Format(goodUsedBinWithScan ? ItemFulfillmentUrl : ItemFulfillmentUrlNoReplace, "vendorReturnAuthorization", orderId);

                string? jsonStringGood = null;
                try
                {
                    var payloadGood = VendorReturnAuthorizationIFPayloadDTO.CreateForItemFulfillment(goodTO, itemFulfillmentStatus, printLabel, isUsedBin);

                    jsonStringGood = JsonSerializer.Serialize(payloadGood, JsonSerializerOption);

                    await MakeRequest<object>(urlGood, jsonStringGood, HttpMethod.Post);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "GOOD VRA Item Fulfillment failed. Url: {Url}. Payload: {Payload}", urlGood, jsonStringGood);
                    var diag = $"[diag replace={urlGood.Contains("replace=")} usedBin={isUsedBin} scannedLines={goodTO.Count(x => x.ScannedQuantity > 0)} totalLines={goodTO.Count}]";
                    throw new Exception($"Error while posting GOOD VRA Item Fulfillment. {ex.Message} {diag}", ex);
                }
            }
            return true;
        }
        #endregion

        #region TripTicket
        public async Task<bool> CancelTripTicket(PostTripTicketDTO Data)
        {
            try
            {
                string url = TripTicketRestletUrl;

                try
                {
                    UpdateTripTicketPayloadDTO payload = UpdateTripTicketPayloadDTO.CancelTripTicket(Data);

                    var jsonStringGood = JsonSerializer.Serialize(payload, JsonSerializerOption);

                    await MakeRequest<object>(url, jsonStringGood, HttpMethod.Put);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error while cancelling Trip Ticket. {ex.Message}", ex);
                }

                return true;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<bool> UpdateTripTicket(PostTripTicketDTO Data, List<ItemFulfillmentDTO> RemovedIF, List<ItemFulfillmentDTO> AddedIF)
        {
            try
            {
                await ChangeShipStatus(Data);

                string url = TripTicketRestletUrl;

                try
                {
                    UpdateTripTicketPayloadDTO payload = UpdateTripTicketPayloadDTO.UpdateTripTicket(Data, RemovedIF, AddedIF);

                    var jsonStringGood = JsonSerializer.Serialize(payload, JsonSerializerOption);

                    await MakeRequest<object>(url, jsonStringGood, HttpMethod.Put);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error while updating Trip Ticket. {ex.Message}", ex);
                }

                return true;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<bool> SaveTripTicket(PostTripTicketDTO Data)
        {
            try
            {
                await ChangeShipStatus(Data);

                string url = TripTicketRestletUrl;

                try
                {
                    TripTicketPayloadDTO payload = TripTicketPayloadDTO.CreateTripTicket(Data);

                    var jsonStringGood = JsonSerializer.Serialize(payload, JsonSerializerOption);

                    await MakeRequest<object>(url, jsonStringGood, HttpMethod.Post);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error while posting Trip Ticket. {ex.Message}", ex);
                }

                return true;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task<bool> ChangeShipStatus(PostTripTicketDTO Data)
        {
            try
            {
                foreach (var itemfulfillments in Data.ItemFulfillments)
                {
                    string url = string.Format(PatchItemFulfillmentUrl, itemfulfillments.NetsuiteOrderInternalId);

                    ItemFulfillmentStatusUpdatePayloadDTO payload = ItemFulfillmentStatusUpdatePayloadDTO.ItemFulfillmentUpdateToShipped();

                    var jsonStringGood = JsonSerializer.Serialize(payload, JsonSerializerOption);

                    await MakeRequest<object>(url, jsonStringGood, HttpMethod.Patch);
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while updating Item Fulfillment status.", ex);
            }
        }

        public string GetRestAPIURI => RestApiRoot;
        public string GetRestletURI => $"https://{AccountId}.restlets.api.netsuite.com/app/site/hosting/restlet.nl";
        #endregion

        #region Inventory Counting
        public async Task<bool> PatchInventoryCounting(List<PatchInventoryCountingDTO> Data)
        {
            var orderId = Data.Select(x => x.NetsuiteOrderInternalId).FirstOrDefault();
            string url = string.Format(PatchInventoryCountUrl, orderId);

            var goodIC = Data.Where(x => !x.IsBad).ToList();

            if (goodIC.Any())
            {
                try
                {
                    var payloadGood = PatchInventoryCountingPayloadDTO.PatchInventoryCounting(goodIC);

                    var jsonStringGood = JsonSerializer.Serialize(payloadGood, JsonSerializerOption);

                    await MakeRequest<object>(url, jsonStringGood, HttpMethod.Patch);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error while posting GOOD Inventory Counting. {ex.Message}", ex);
                }
            }

            return true;
        }

        public async Task<bool> PostInventoryWorksheet(List<InventoryWorksheetLineDTO> Data, int Location, int Subsidiary)
        {
            string url = InventoryWorksheetRestletUrl;

            try
            {
                var payloadGood = PostInventoryWorksheetPayload.PostInventoryWorksheet(Data, Location, Subsidiary);

                var jsonStringGood = JsonSerializer.Serialize(payloadGood, JsonSerializerOption);

                await MakeRequest<object>(url, jsonStringGood, HttpMethod.Post);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error while posting Inventory Worksheet. {ex.Message}", ex);
            }

            return true;
        }
        #endregion

    }
}
