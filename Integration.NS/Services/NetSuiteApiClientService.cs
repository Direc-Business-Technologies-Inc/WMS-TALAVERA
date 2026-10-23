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

        /// <summary>
        /// Identifies the running server build. It is embedded in the receiving diagnostics
        /// string so a field failure can be tied to the deployed assembly without a logs round
        /// trip. The mobile build is fingerprinted by the diag's scannedLines / totalLines
        /// counts, but only in one direction: a payload that carries unscanned fillers reports
        /// totalLines larger than scannedLines. Equal counts are inconclusive - a fully-scanned
        /// order reports them equal on either build - so do not treat equality as proof of a
        /// stale client.
        /// </summary>
        internal static readonly string BuildStamp =
            typeof(NetSuiteApiClientService).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? typeof(NetSuiteApiClientService).Assembly.GetName().Version?.ToString()
            ?? "unknown";

        /// <summary>
        /// Public read-only view of <see cref="BuildStamp"/> for the startup log
        /// (deploy verification without waiting for a field failure).
        /// </summary>
        public static string BuildStampValue => BuildStamp;

        private static readonly string ItemFulfillmentUrl = $"https://{AccountId}.suitetalk.api.netsuite.com/services/rest/record/v1/{{0}}/{{1}}/!transform/itemFulfillment?replace=item.inventoryDetail.inventoryAssignment";
        private static readonly string ItemFulfillmentUrlNotUsedBin = $"https://{AccountId}.suitetalk.api.netsuite.com/services/rest/record/v1/{{0}}/{{1}}/!transform/itemFulfillment?replace=item.inventoryDetail.inventoryAssignment";
        private static readonly string ItemFulfillmentUrlNoReplace = $"https://{AccountId}.suitetalk.api.netsuite.com/services/rest/record/v1/{{0}}/{{1}}/!transform/itemFulfillment";

        private static readonly string PatchItemFulfillmentUrl = $"https://{AccountId}.suitetalk.api.netsuite.com/services/rest/record/v1/itemFulfillment/{{0}}";

        private static readonly string PatchInventoryCountUrl = $"https://{AccountId}.suitetalk.api.netsuite.com/services/rest/record/v1/inventorycount/{{0}}";

        private static readonly string ItemReceiptUrl = $"https://{AccountId}.suitetalk.api.netsuite.com/services/rest/record/v1/{{0}}/{{1}}/!transform/itemReceipt";

        private static readonly string UpdateRecordUrl = $"https://{AccountId}.suitetalk.api.netsuite.com/services/rest/record/v1/{{0}}/{{1}}";

        private static readonly string ClientCredentialsCertificateId = Environment.GetEnvironmentVariable("NETSUITE_CERTIFICATE_ID") ?? string.Empty;

        private static string ItemReceiptRestletUrl = $"https://{AccountId}.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=1853&deploy=1";

        private static string ItemFulfillmentRestletUrl = $"https://{AccountId}.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=1863&deploy=1";

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
            string? inventoryWorksheetRestlet = null,
            string? itemFulfillmentRestlet = null)
        {
            if (suiteQlRoot is not null) SuiteQLRoot = suiteQlRoot;
            if (recordApiRoot is not null) RecordApiRoot = recordApiRoot;
            if (itemReceiptRestlet is not null) ItemReceiptRestletUrl = itemReceiptRestlet;
            if (tripTicketRestlet is not null) TripTicketRestletUrl = tripTicketRestlet;
            if (inventoryWorksheetRestlet is not null) InventoryWorksheetRestletUrl = inventoryWorksheetRestlet;
            if (itemFulfillmentRestlet is not null) ItemFulfillmentRestletUrl = itemFulfillmentRestlet;
        }

        internal static void ResetUrlRootsForTests()
        {
            SuiteQLRoot = $"{RestApiRoot}/query/v1/suiteql";
            RecordApiRoot = $"{RestApiRoot}/record/v1";
            ItemReceiptRestletUrl = $"https://{AccountId}.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=1853&deploy=1";
            TripTicketRestletUrl = $"https://{AccountId}.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=1862&deploy=1";
            InventoryWorksheetRestletUrl = $"https://{AccountId}.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=1878&deploy=1";
            ItemFulfillmentRestletUrl = $"https://{AccountId}.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=2522&deploy=1";
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

        /// <summary>
        /// Normalises a SuiteQL statement for transport: strips comments first,
        /// then collapses every whitespace run (newlines included) to a single
        /// space. The order is load-bearing — folding newlines first would turn
        /// each <c>--</c> line comment in a script into a comment swallowing the
        /// entire remainder of the statement, which NetSuite reports as 400
        /// "end of input ... no_root_node" with no offending token named.
        /// See <see cref="Helpers.SuiteQLText.StripComments"/>.
        /// </summary>
        string FormatQuery(string query)
        {
            return Regex.Replace(Helpers.SuiteQLText.StripComments(query), @"\s+", " ").Trim();
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
            var result = await NetsuiteQueryWithMetadata<T>(queryName, parameters, limit, offset, ct);

            return result?.items;
        }

        /// <summary>
        /// Same script execution as <see cref="NetsuiteQuery{T}"/>, but keeps the
        /// response envelope so a caller that spans pages can stop on the
        /// authoritative <c>totalResults</c>/<c>hasMore</c> instead of probing
        /// one page past the end. NetSuite rejects an offset equal to the result
        /// count with 404 INVALID_PARAMETER, so an exact multiple of the page
        /// size would otherwise fail on a "next page" that can never exist.
        /// </summary>
        public async Task<NetSuiteResponse<T>?> NetsuiteQueryWithMetadata<T>(
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

            return await MakeRequest<NetSuiteResponse<T>>(url, jsonBody, HttpMethod.Post, ct: ct);
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

            // A PO line that is already fully received is absent from the transform's item
            // sublist, so any orderLine reference for it - the scanned entry or the zero-qty
            // filler the payload builder emits - is an invalid insert and NetSuite rejects the
            // whole save. Strip such lines before bucketing so the payload builder only ever
            // receives postable lines, mirroring SaveTOItemFulfillment.
            var scannedButClosed = Data.Where(x => x.ScannedQuantity > 0 && IsClosedPOLine(x)).ToList();
            Data = Data.Where(x => !IsClosedPOLine(x)).ToList();

            // A stale client that scanned only already-received lines must not be told the post
            // succeeded: nothing is left to receive, so fail with the offending lines named.
            if (scannedButClosed.Any() && !Data.Any(x => x.ScannedQuantity > 0))
            {
                var detail = string.Join("; ", scannedButClosed.Select(x =>
                    $"line {x.LineSequenceNumber} ({x.MaterialCode}) - scanned {x.ScannedQuantity}, remaining {PORemainingDisplayQuantity(x)}"));
                throw new Exception($"Cannot post PO Item Receipt for order {orderId}: the scanned line(s) are already fully received ({detail}). Reload the order and rescan the affected line(s); see the receipts already posted against this purchase order in NetSuite.");
            }

            // Reject scans beyond the remaining open quantity before the first NetSuite call: on
            // a partially received order the recorded quantity is not the open quantity, so
            // surface the WMS line and the real ordered/remaining figures to the operator instead
            // of a cryptic NetSuite 400 mid-post.
            var overRemaining = FindPOScansExceedingRemaining(Data);
            if (overRemaining.Count > 0)
            {
                throw new Exception($"Cannot post PO Item Receipt: scanned quantity exceeds the remaining quantity for: {string.Join("; ", overRemaining)}. Reload the order and rescan the affected line(s).");
            }

            // replace + explicit inventoryDetail only for bin-tracked locations; non-bin
            // posts keep the plain transform. Without ?replace=item.inventoryDetail.inventoryAssignment
            // NetSuite ignores the inventoryDetail carried in the body and auto-generates an
            // empty assignment, failing bin-tracked lines with "Please configure the inventory detail in line N".
            var isUsedBin = Data.Any(x => x.IsLocationUsedBin);
            string url = BuildPOItemReceiptUrl(isUsedBin, orderId);

            // A scanned line at a bin-tracked location whose inventoryDetail carries no binNumber
            // is rejected by NetSuite with "configure the inventory detail in line N". Fail here,
            // naming the WMS line, material and location, instead of surfacing that cryptic 400
            // after the post.
            var missingBin = FindPOMissingBinLines(Data);

            if (missingBin.Count > 0)
            {
                throw new Exception($"Cannot post PO Item Receipt: scanned lines at bin-tracked locations have no resolved bin: {string.Join("; ", missingBin)}. Reload the order; if the bin is still missing, ask Master Data to set a Vendor Bin Assignment or a preferred bin for these items.");
            }

            var badPO = Data.Where(x => x.IsBad && x.ScannedQuantity > 0).ToList();

            if (badPO.Any())
            {
                string? jsonStringBad = null;
                try
                {
                    var payloadBad = PurchaseOrderIRPayloadDTO.CreateForItemReceipt(Data, userId, 2, remarks, isUsedBin);

                    jsonStringBad = JsonSerializer.Serialize(payloadBad, JsonSerializerOption);

                    await MakeRequest<object>(url, jsonStringBad, HttpMethod.Post);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "BAD PO Item Receipt failed. Url: {Url}. Payload: {Payload}", url, jsonStringBad);
                    var diag = $"[diag replace={url.Contains("replace=")} usedBin={isUsedBin} scannedLines={badPO.Count(x => x.ScannedQuantity > 0)} totalLines={Data.Count} build={BuildStamp}]"
                        + DecodeItemReceiptLine(ex.Message, Data, isGoodPost: false);
                    throw new Exception($"Error while posting BAD PO Item Receipt. {ex.Message} {diag}", ex);
                }
            }

            var goodPO = Data.Where(x => !x.IsBad && x.ScannedQuantity > 0).ToList();

            if (goodPO.Any())
            {
                string? jsonStringGood = null;
                try
                {
                    var payloadGood = PurchaseOrderIRPayloadDTO.CreateForItemReceipt(Data, userId, 1, remarks, isUsedBin);

                    jsonStringGood = JsonSerializer.Serialize(payloadGood, JsonSerializerOption);

                    await MakeRequest<object>(url, jsonStringGood, HttpMethod.Post);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "GOOD PO Item Receipt failed. Url: {Url}. Payload: {Payload}", url, jsonStringGood);
                    var diag = $"[diag replace={url.Contains("replace=")} usedBin={isUsedBin} scannedLines={goodPO.Count(x => x.ScannedQuantity > 0)} totalLines={Data.Count} build={BuildStamp}]"
                        + DecodeItemReceiptLine(ex.Message, Data, isGoodPost: true);
                    throw new Exception($"Error while posting GOOD PO Item Receipt. {ex.Message} {diag}", ex);
                }
            }

            return true;
        }

        /// <summary>
        /// The PO item receipt <c>!transform</c> URL. A bin-tracked location must declare
        /// <c>?replace=item.inventoryDetail.inventoryAssignment</c> so NetSuite replaces its
        /// pre-generated (empty) inventory assignment with the bin the payload carries;
        /// without it the body's <c>inventoryDetail</c> is ignored and the save is rejected
        /// with "Please configure the inventory detail in line N". Non-bin locations need no
        /// detail, so they keep the plain transform - the same split the Returns receipt uses.
        /// </summary>
        internal static string BuildPOItemReceiptUrl(bool isUsedBin, int orderId) =>
            string.Format(
                isUsedBin ? ItemReceiptUrl + "?replace=item.inventoryDetail.inventoryAssignment" : ItemReceiptUrl,
                "purchaseOrder",
                orderId);

        /// <summary>
        /// A PO line with no remaining open quantity is absent from the item sublist of the
        /// <c>!transform/itemReceipt</c> response, so addressing it (a scanned entry OR a
        /// zero-qty filler) is an invalid insert on a static sublist and NetSuite rejects the
        /// whole save with "Please configure the inventory detail in line N". The item-receipt
        /// transform has the same shape as the itemFulfillment one guarded by
        /// <see cref="IsClosedFulfillmentLine"/>, and takes the same remedy.
        /// </summary>
        /// <remarks>
        /// Remaining is <c>LineQuantity - LineQuantityReceived</c> (transactionline.
        /// quantityshiprecv), the open quantity on a partially received order. It deliberately
        /// does NOT consult a per-line closed flag: a fully received purchase-order line still
        /// reports isclosed = F (verified via SuiteQL on PO-THM00100000216-TR, 2026-10-17, where
        /// all 18 lines had qty == recvd and isclosed = F), so the flag alone would let the
        /// failing post through.
        /// </remarks>
        internal static bool IsClosedPOLine(PostPurchaseOrderDTO line) =>
            line.LineQuantity - line.LineQuantityReceived <= 0;

        /// <summary>
        /// Remaining open quantity in the operator's display UoM, which is what
        /// <c>ScannedQuantity</c> is expressed in. PO quantities arrive in the item's stock UoM,
        /// so the base-unit remainder is divided by the line's UoM rate.
        /// </summary>
        internal static decimal PORemainingDisplayQuantity(PostPurchaseOrderDTO line)
        {
            var rate = line.UoMRate > 0 ? line.UoMRate : 1m;
            return (line.LineQuantity - line.LineQuantityReceived) / rate;
        }

        /// <summary>
        /// Scans beyond the line's remaining open quantity cannot be received: after an earlier
        /// partial receipt only <c>LineQuantity - LineQuantityReceived</c> is still open. Returns
        /// one descriptor per offending line, naming the item-list line, material and the ordered
        /// / remaining figures, so the caller can fail with an actionable message before the first
        /// NetSuite call.
        /// </summary>
        internal static List<string> FindPOScansExceedingRemaining(List<PostPurchaseOrderDTO> lines)
        {
            var offenders = new List<string>();

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.ScannedQuantity <= 0) continue;

                var remaining = PORemainingDisplayQuantity(line);
                if (line.ScannedQuantity > remaining)
                {
                    offenders.Add(
                        $"item-list line {i + 1} (WMS line {line.LineSequenceNumber}) {line.MaterialCode} - {line.MaterialName}: scanned {line.ScannedQuantity}, ordered {line.LineQuantity}, remaining {remaining}");
                }
            }

            return offenders;
        }

        /// <summary>
        /// Names every scanned line that will post to a bin-tracked location without a
        /// resolvable bin. A purchase order can carry lines at different locations, so the bin
        /// requirement is judged on the line's own <see cref="PostPurchaseOrderDTO.IsLocationUsedBin"/>,
        /// never on an order-level flag - otherwise a non-bin line on a mixed-location PO is
        /// falsely rejected. The grouping mirrors the payload builder: one representative per
        /// (line, quality) bucket, because the good and bad posts are separate NetSuite calls
        /// and each can fail on its own row.
        /// </summary>
        internal static List<string> FindPOMissingBinLines(List<PostPurchaseOrderDTO> lines) =>
            lines
                .Where(x => x.ScannedQuantity > 0)
                .GroupBy(x => (x.LineSequenceNumber, x.IsBad))
                .Select(g => g
                    .OrderByDescending(x => x.ScannedQuantity > 0)
                    .ThenByDescending(x => x.ScannedQuantity)
                    .First())
                .Where(x => RequiresBin(x.IsLocationUsedBin, x.ScannedQuantity, x.VendorBinAssignmentId, x.NetsuiteMaterialPrefferedBinId))
                .Select(x => $"WMS line {x.LineSequenceNumber} ({x.MaterialCode} - {x.MaterialName}) at {x.LocationName}")
                .ToList();

        /// <summary>
        /// Decodes NetSuite's "line N of the item list" rejection into the offending DTO row so a
        /// field failure identifies the item without a server-logs round trip, mirroring the item
        /// fulfillment decoder. Returns an empty string when the message does not carry the marker.
        /// </summary>
        /// <remarks>
        /// The index is 1-based and addresses the payload's <c>item.items</c> sublist, which the
        /// builder produces by grouping the WHOLE list by LineSequenceNumber in first-appearance
        /// order (one entry per group), so the caller must pass the same list it handed to
        /// <c>CreateForItemReceipt</c> - not the good/bad bucket. The per-group representative and
        /// the tie-break below are kept identical to that builder; indexing the raw, ungrouped
        /// list would drift on a fanned-out or good/bad split line.
        /// </remarks>
        internal static string DecodeItemReceiptLine(string message, List<PostPurchaseOrderDTO> lines, bool isGoodPost)
        {
            var match = Regex.Match(message, @"line (\d+) of the item list");
            if (!match.Success || !int.TryParse(match.Groups[1].Value, out var itemListLine)) return string.Empty;

            var postable = lines
                .GroupBy(x => x.LineSequenceNumber)
                .Select(group =>
                {
                    var postBucket = isGoodPost
                        ? group.Where(x => !x.IsBad)
                        : group.Where(x => x.IsBad);

                    return postBucket
                            .OrderByDescending(x => x.ScannedQuantity > 0)
                            .ThenByDescending(x => x.ScannedQuantity)
                            .FirstOrDefault()
                        ?? group
                            .OrderByDescending(x => x.ScannedQuantity > 0)
                            .ThenByDescending(x => x.ScannedQuantity)
                            .First();
                })
                .ToList();

            if (itemListLine < 1 || itemListLine > postable.Count) return string.Empty;

            var offender = postable[itemListLine - 1];
            return $" | line{itemListLine}=WMSLine {offender.LineSequenceNumber} {offender.MaterialCode} - {offender.MaterialName} usedBin={offender.IsLocationUsedBin} vendorBin={offender.VendorBinAssignmentId} preferredBin={offender.NetsuiteMaterialPrefferedBinId} qty={offender.ScannedQuantity}";
        }

        public async Task<bool> SaveTOItemReceipt(List<PostTransferOrderDTO> Data, int TONetsuiteOrderInternalId, int userId, string remarks)
        {
            // The receiving line query fans one TO line into several rows when the item has
            // multiple preferred bins at the location or the vendor has multiple bin-assignment
            // records there, and the mobile posts every row. Collapse the fan-out here so each
            // orderLine appears once per (quality, missing) bucket — the restlet contract
            // requires a unique orderLine in the body.
            Data = DeduplicateReceivingLines(Data, x => x.LineSequenceNumber, x => x.IsBad, x => x.IsMissing, x => x.ScannedQuantity);

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

        /// <summary>
        /// Collapses the returns rows of one bucket (good/bad or missing) to one entry per
        /// LineSequenceNumber. ReturnsIRPayloadDTO.CreateForItemReceipt maps each row 1:1 to
        /// item[].orderline, and NetSuite rejects a body that repeats an orderLine with
        /// [DUPLICATE_KEYS]. The returns line query fans a transaction line out when the item
        /// has several preferred bins at the location or the vendor has several assignment
        /// records there, and the mobile used to resend a partial save that had already been
        /// posted. Identical copies merge silently, preferring the row the operator scanned and
        /// then one that carries a bin; copies that disagree on quantity would post a wrong
        /// quantity, so the save fails fast instead of guessing. The key never includes
        /// IsMissing: a partially scanned line legitimately has a good row and a missing row,
        /// and they go to separate Item Receipt documents, so callers pass one bucket at a
        /// time.
        /// </summary>
        internal static List<PostReturnsDTO> CollapseReturnLines(List<PostReturnsDTO> lines)
        {
            return lines
                .GroupBy(x => x.LineSequenceNumber)
                .Select(g =>
                {
                    // Zero-quantity copies are the fillers the payload builder emits to keep
                    // the ?replace=item.inventoryDetail.inventoryAssignment sub-resource on
                    // every entry, so they never count as a disagreement on quantity.
                    var quantities = g
                        .Where(x => x.ScannedQuantity > 0)
                        .Select(x => x.ScannedQuantity)
                        .Distinct()
                        .ToList();

                    if (quantities.Count > 1)
                    {
                        var duplicate = g.First();

                        throw new InvalidOperationException(
                            $"Returns line {duplicate.LineSequenceNumber} ({duplicate.MaterialCode} - {duplicate.MaterialName}) " +
                            $"was sent with diverging quantities ({string.Join(", ", quantities)}). " +
                            "Refresh the document and scan the line again.");
                    }

                    return g
                        .OrderByDescending(x => x.ScannedQuantity > 0)
                        .ThenByDescending(x => x.NetsuiteMaterialVendorAssignedBin != 0)
                        .ThenByDescending(x => x.NetsuiteMaterialPrefferedBinId != 0)
                        .First();
                })
                .ToList();
        }

        /// <summary>
        /// Tolerance for the scanned/missing comparison. Quantities are rounded to five
        /// decimals by the mobile (FormatHelper.RoundOfNearestHundredThousands), so an exact
        /// comparison would reject legitimate saves because of the division by UoMRate.
        /// </summary>
        internal const decimal ReturnQuantityTolerance = 0.00001m;

        /// <summary>
        /// Per line, the posted quantity is what the operator scanned plus, when they chose to
        /// tag the remainder missing, what is written off as missing. Both land on the same
        /// substransaction line, so together they must not exceed the line's remaining open
        /// quantity. This guards the silent over-receipt the fan-out caused: a duplicate row
        /// made a fully scanned line look unscanned and posted its remainder as missing.
        /// The figures are the ones the mobile read when it loaded the document, so this
        /// detects an inconsistent request, not a stale device (NetSuite still owns that).
        /// Returns one descriptor per offender so the caller fails before the first POST.
        /// </summary>
        internal static List<string> FindReturnQuantityOverflows(
            List<PostReturnsDTO> goodLines,
            List<PostReturnsDTO> missingLines)
        {
            var offenders = new List<string>();

            var lines = goodLines
                .Concat(missingLines)
                .GroupBy(x => x.LineSequenceNumber)
                .Select(g => g.First());

            foreach (var line in lines)
            {
                var good = goodLines
                    .Where(x => x.LineSequenceNumber == line.LineSequenceNumber)
                    .Sum(x => x.ScannedQuantity);

                var missing = missingLines
                    .Where(x => x.LineSequenceNumber == line.LineSequenceNumber)
                    .Sum(x => x.ScannedQuantity);

                var posted = good + missing;

                if (line.UoMRate <= 0)
                {
                    offenders.Add($"WMS line {line.LineSequenceNumber} {line.MaterialCode} - {line.MaterialName}: UoM rate is {line.UoMRate}, remaining quantity cannot be verified");

                    continue;
                }

                var remaining = (line.LineQuantity - (line.LineQuantityReceived + line.LineQuantityBackOrdered)) / line.UoMRate;

                if (posted > remaining + ReturnQuantityTolerance)
                {
                    offenders.Add($"WMS line {line.LineSequenceNumber} {line.MaterialCode} - {line.MaterialName}: scanned {good} + missing {missing} = {posted}, remaining {remaining}");
                }
            }

            return offenders;
        }

        public async Task<bool> SaveReturnsItemReceipt(List<PostReturnsDTO> Data, int TONetsuiteOrderInternalId, int userId, string remarks)
        {
            var orderId = Data.Select(x => x.NetsuiteOrderInternalId).FirstOrDefault();
            string url = string.Format(ItemReceiptUrl + "?replace=item.inventoryDetail.inventoryAssignment", "transferOrder", TONetsuiteOrderInternalId);
            //string url = string.Format(ItemReceiptUrl, "transferOrder", TONetsuiteOrderInternalId);

            var transferCategory = (TransferCategory)Data.Select(x => x.TransferCategory).FirstOrDefault();

            var receivingCategory = transferCategory == TransferCategory.GoodItems ? 1 : 2;

            // Collapse before the payloads are built: one item[].orderline per bucket is the
            // NetSuite contract, and this is where the mobile's duplicate rows used to land.
            var returns = CollapseReturnLines(Data.Where(x => !x.IsMissing).ToList());
            var missingReturns = CollapseReturnLines(Data.Where(x => x.IsMissing).ToList());

            var overflows = FindReturnQuantityOverflows(returns, missingReturns);
            if (overflows.Count > 0)
            {
                throw new InvalidOperationException(
                    "Refusing to post Returns Item Receipt. " + string.Join(" | ", overflows));
            }

            var bucketName = receivingCategory == 1 ? "GOOD" : "BAD";

            if (returns.Any(x => x.ScannedQuantity > 0))
            {
                var jsonStringGood = string.Empty;

                try
                {
                    ReturnsIRPayloadDTO payloadGood = ReturnsIRPayloadDTO.CreateForItemReceipt(returns, receivingCategory, orderId, userId, remarks);

                    jsonStringGood = JsonSerializer.Serialize(payloadGood, JsonSerializerOption);

                    await MakeRequest<object>(url, jsonStringGood, HttpMethod.Post);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{Bucket} Returns Item Receipt failed. Url: {Url}. Payload: {Payload}", bucketName, url, jsonStringGood);

                    throw new Exception($"Error while posting {bucketName} Returns Item Receipt. {ex.Message}", ex);
                }
            }

            if (missingReturns.Any(x => x.ScannedQuantity > 0))
            {
                var jsonStringMissing = string.Empty;

                try
                {
                    ReturnsIRPayloadDTO payloadMissing = ReturnsIRPayloadDTO.CreateForItemReceipt(missingReturns, 5, orderId, userId, remarks);

                    jsonStringMissing = JsonSerializer.Serialize(payloadMissing, JsonSerializerOption);

                    await MakeRequest<object>(url, jsonStringMissing, HttpMethod.Post);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "MISSING Returns Item Receipt failed. Url: {Url}. Payload: {Payload}", url, jsonStringMissing);

                    throw new Exception($"Error while posting MISSING Returns Item Receipt. {ex.Message}", ex);
                }
            }

            return true;
        }
        #endregion

        #region Itemfulfillment

        /// <summary>
        /// The packing line queries (NS_TO_x_Packing_Get_Items etc.) join one transaction
        /// line to the item's preferred bin and the preferred vendor's bin assignment.
        /// Either join multiplies the row when the item has several preferred bins at the
        /// location or the vendor has several assignment records there, and the mobile
        /// posts every row back. Two body entries with the same orderLine are invalid for
        /// the !transform/itemFulfillment POST: under ?replace=item.inventoryDetail.inventoryAssignment
        /// the zero-qty filler copy's EMPTY assignment collides with the scanned copy's
        /// assignment and NetSuite rejects with "Please configure the inventory detail in
        /// line N of the item list" naming the scanned line. Collapse each (line, quality)
        /// pair to one row, preferring the row the operator actually scanned; good/bad
        /// split scans of one line (same LineSequenceNumber, different IsBad) are
        /// legitimate and are kept. GroupBy preserves first-appearance order, so the
        /// posted body order (and the line-N decoder below) stays aligned.
        /// </summary>
        internal static List<T> DeduplicateFulfillmentLines<T>(
            List<T> lines,
            Func<T, int> lineSequenceNumber,
            Func<T, bool> isBad,
            Func<T, decimal> scannedQuantity)
        {
            return lines
                .GroupBy(x => (lineSequenceNumber(x), isBad(x)))
                .Select(g => g
                    .OrderByDescending(x => scannedQuantity(x) > 0)
                    .ThenByDescending(scannedQuantity)
                    .First())
                .ToList();
        }

        /// <summary>
        /// Receiving counterpart of <see cref="DeduplicateFulfillmentLines"/>. The receiving
        /// line queries (NS_TO_Get_Itemfulfillment_Items.sql and siblings) join the item's
        /// preferred bin and the preferred vendor's bin assignment; either join fans one
        /// transaction line into several rows when the item has multiple preferred bins at
        /// the location or the vendor has multiple assignment records there, and the mobile
        /// posts every row. TransferOrderIRRestletPayloadDTO.CreateForItemReceiptRestlet maps
        /// each row 1:1 into lines[], so the duplicate orderLine breaks the restlet save.
        /// Collapse each (orderLine, quality, missing) triple to one row, preferring the row
        /// the operator actually scanned.
        /// Unlike <see cref="DeduplicateFulfillmentLines"/> this key MUST include IsMissing:
        /// a partially-scanned line legitimately produces BOTH a good entry and a missing
        /// entry with the same orderLine and the same IsBad, and they must survive into their
        /// separate good/missing buckets. GroupBy preserves first-appearance order.
        /// </summary>
        internal static List<T> DeduplicateReceivingLines<T>(
            List<T> lines,
            Func<T, int> lineSequenceNumber,
            Func<T, bool> isBad,
            Func<T, bool> isMissing,
            Func<T, decimal> scannedQuantity)
        {
            return lines
                .GroupBy(x => (lineSequenceNumber(x), isBad(x), isMissing(x)))
                .Select(g => g
                    .OrderByDescending(x => scannedQuantity(x) > 0)
                    .ThenByDescending(scannedQuantity)
                    .First())
                .ToList();
        }

        /// <summary>
        /// True when a posted line must carry a bin. Only bin-tracked locations require one.
        /// </summary>
        /// <remarks>
        /// Callers pass the flag that applies to the line being posted. The transfer-order paths
        /// pass the order-level flag because an order has one location; the purchase-order paths
        /// pass the line's own flag because a PO can carry lines at different locations. Both bin
        /// ids reach the WMS through LEFT JOINs in the queries, so a non-bin-tracked location
        /// resolves them to 0 by construction - hence the flag gate, which keeps a legitimate
        /// save from being told the location is bin-tracked when it is not (e.g. SELLING AREA -
        /// CAUAYAN, where usebins = 'F'). Passed per line, it also avoids falsely rejecting a
        /// non-bin line on a mixed-location PO.
        ///
        /// Zero-quantity fillers are exempt by design: <c>scannedQuantity &gt; 0</c> keeps them
        /// out of the bin requirement. That is correct, not an oversight - a filler ships
        /// nothing, carries an EMPTY assignment, and has no bin to name at a bin-tracked
        /// location. Do not "fix" this by requiring a bin on fillers: an unscanned line has no
        /// operator-chosen bin, and rejecting it would block the very save reconciliation exists
        /// to complete.
        /// </remarks>
        internal static bool RequiresBin(bool isUsedBin, decimal scannedQuantity, int vendorAssignedBin, int preferredBin) =>
            isUsedBin && scannedQuantity > 0 && vendorAssignedBin == 0 && preferredBin == 0;

        /// <summary>
        /// A TO line that is closed or has no remaining open quantity is absent
        /// from the item sublist of the <c>!transform/itemFulfillment</c>
        /// response, so addressing it (a scanned entry OR a zero-qty filler)
        /// is an invalid insert on a static sublist and NetSuite rejects the
        /// whole save. Remaining is <c>LineQuantity - QuantityAlreadyShipped</c>
        /// (transactionline.quantityshiprecv), the correct open quantity on a
        /// partially fulfilled order - not the quantitypacked-based figure.
        /// </summary>
        internal static bool IsClosedFulfillmentLine(PostTransferOrderDTO line) =>
            line.IsLineClosed || line.RemainingQuantity <= 0;

        /// <summary>
        /// Scans beyond the line's remaining open quantity cannot be fulfilled:
        /// after an earlier partial fulfillment only <c>LineQuantity -
        /// QuantityAlreadyShipped</c> is still shippable. Returns one descriptor
        /// per offending line, naming the WMS line, material and the ordered /
        /// remaining figures, so the caller can fail with an actionable message
        /// before the first NetSuite call.
        /// </summary>
        /// <remarks>
        /// ScannedQuantity accumulates in the line's DISPLAY unit (a barcode
        /// contributes barcodeRate / lineRate), while LineQuantity,
        /// QuantityAlreadyShipped, LineQuantityPacked and LineQuantityBackOrdered
        /// are raw stock units (transactionline.quantity). The comparison must
        /// therefore divide by the line's UoM conversion rate
        /// (<see cref="PackableDisplayQuantity"/>). The pre-2026-10-23 guard
        /// compared display-unit scans against raw stock units, so a BOX12 line
        /// accepted 12x its remaining quantity - the over-scan report on
        /// STR-THM00100000867 (item 00032678: quantity 888 eaches, rate 12,
        /// guard ceiling 888 instead of 74 boxes).
        /// </remarks>
        internal const decimal ScanQuantityTolerance = 0.00001m;

        /// <summary>
        /// The quantity one TO line may still be packed, expressed in the SAME
        /// display unit <see cref="PostTransferOrderDTO.ScannedQuantity"/>
        /// accumulates in. Backordered quantity is unavailable stock and is not
        /// packable; a fully backordered line therefore packs 0 and refuses
        /// every scan. Zero/absent UoM rates fall back to 1 so legacy payloads
        /// without a rate keep their previous behaviour.
        /// </summary>
        internal static decimal PackableDisplayQuantity(PostTransferOrderDTO line)
        {
            var rate = line.UoMRate > 0 ? line.UoMRate : 1m;
            var committed = Math.Max(line.LineQuantityPacked, line.QuantityAlreadyShipped);
            var remaining = line.LineQuantity - committed - line.LineQuantityBackOrdered;
            return remaining > 0 ? remaining / rate : 0m;
        }

        internal static List<string> FindScansExceedingRemaining(List<PostTransferOrderDTO> lines)
        {
            var offenders = new List<string>();

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.ScannedQuantity <= 0) continue;

                var remaining = PackableDisplayQuantity(line);
                if (line.ScannedQuantity > remaining + ScanQuantityTolerance)
                {
                    offenders.Add(
                        $"item-list line {i + 1} (WMS line {line.LineSequenceNumber}) {line.MaterialCode} - {line.MaterialName}: scanned {line.ScannedQuantity}, ordered {line.LineQuantity}, remaining {remaining}");
                }
            }

            return offenders;
        }

        /// <summary>
        /// Good and bad scans of the SAME line live in different payload rows, so
        /// a per-row check passes each one individually while their sum exceeds
        /// what the line can still pack. Groups by WMS line and flags the combined
        /// total; single-row groups are skipped because
        /// <see cref="FindScansExceedingRemaining"/> already names those (with the
        /// item-list position).
        /// </summary>
        internal static List<string> FindLinesExceedingRemainingAcrossBuckets(
            IReadOnlyCollection<PostTransferOrderDTO> lines)
        {
            var offenders = new List<string>();

            foreach (var group in lines
                         .Where(x => x.ScannedQuantity > 0)
                         .GroupBy(x => x.LineSequenceNumber)
                         .Where(g => g.Count() > 1))
            {
                var line = group.First();
                var total = group.Sum(x => x.ScannedQuantity);
                var remaining = PackableDisplayQuantity(line);

                if (total > remaining + ScanQuantityTolerance)
                {
                    var buckets = string.Join(
                        "+",
                        group.OrderByDescending(x => x.ScannedQuantity)
                            .Select(x => $"{(x.IsBad ? "BAD" : "GOOD")} {x.ScannedQuantity}"));

                    offenders.Add(
                        $"WMS line {group.Key} {line.MaterialCode} - {line.MaterialName}: scanned {total} ({buckets}), remaining {remaining}");
                }
            }

            return offenders;
        }

        /// <summary>
        /// The save-time quantity gate shared by the transform and RESTlet
        /// transports: per-row over-remaining descriptors plus the good+bad
        /// aggregate, both measured in the line's display unit.
        /// </summary>
        internal static void EnsureScansWithinPackableQuantity(IReadOnlyCollection<PostTransferOrderDTO> lines)
        {
            var offenders = FindScansExceedingRemaining(lines.ToList())
                .Concat(FindLinesExceedingRemainingAcrossBuckets(lines))
                .ToList();

            if (offenders.Count == 0) return;

            throw new Exception($"Cannot post TO Item Fulfillment: scanned quantity exceeds the remaining quantity for: {string.Join("; ", offenders)}. Reload the order and rescan the affected line(s).");
        }

        /// <summary>
        /// Fresh line state for one transfer-order SHIPPING line, read straight
        /// from NetSuite at save time. Carries the same figures the device's
        /// load-time snapshot carries on <see cref="PostTransferOrderDTO"/> -
        /// ordered quantity, already-shipped quantity and the closed flag - in the
        /// same raw stock units the packing script (<c>NS_TO_x_Packing_Get_Items</c>)
        /// uses, so the two are directly comparable. The item identity is included
        /// so the server can build a filler for a line the mobile never loaded
        /// (a vendor-scoped read, or a line the display query dropped).
        /// </summary>
        internal sealed class TOFulfillmentFreshLineState
        {
            public int LineId { get; set; }
            public int Item { get; set; }
            public string ItemCode { get; set; } = string.Empty;
            public string ItemName { get; set; } = string.Empty;
            public decimal LineQuantity { get; set; }
            public decimal QuantityShipped { get; set; }
            public decimal QuantityPacked { get; set; }
            public decimal QuantityBackOrdered { get; set; }
            public string IsClosed { get; set; } = "F";
            public decimal UoMRate { get; set; } = 1m;

            /// <summary>
            /// A line the transform exposes on the item sublist: not closed and
            /// with quantity still to ship.
            /// </summary>
            public bool IsOpen =>
                !string.Equals(IsClosed, "T", StringComparison.OrdinalIgnoreCase)
                && (LineQuantity - QuantityShipped) > 0;
        }

        /// <summary>
        /// Reads an id that NetSuite may return as a bare number, a numeric string, or a
        /// reference object (<c>{"id":"1001"}</c>). A strict <c>int?</c> binding throws on the
        /// last two shapes, which made the transform-preview read fail open in the field
        /// (2026-10-23, order 226006: <c>transform=-1</c> with no diagnostic reason).
        /// </summary>
        public sealed class LenientInt32Converter : JsonConverter<int?>
        {
            public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.Null:
                        return null;
                    case JsonTokenType.Number:
                        return reader.TryGetInt32(out var number) ? number : null;
                    case JsonTokenType.String:
                        return int.TryParse(reader.GetString(), out var parsed) ? parsed : null;
                    case JsonTokenType.StartObject:
                        using (var document = JsonDocument.ParseValue(ref reader))
                        {
                            if (!document.RootElement.TryGetProperty("id", out var id)) return null;
                            return id.ValueKind switch
                            {
                                JsonValueKind.Number when id.TryGetInt32(out var objectNumber) => objectNumber,
                                JsonValueKind.String when int.TryParse(id.GetString(), out var objectParsed) => objectParsed,
                                _ => null
                            };
                        }
                    default:
                        reader.Skip();
                        return null;
                }
            }

            public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
            {
                if (value.HasValue) writer.WriteNumberValue(value.Value); else writer.WriteNullValue();
            }
        }

        /// <summary>
        /// One line of the <c>!transform/itemFulfillment</c> preview. <see cref="OrderLine"/>
        /// is the SOURCE transfer-order line id - the same id the POST's <c>orderLine</c>
        /// addresses. <see cref="Line"/> is only the preview's sublist position and must
        /// never be used as an id; a preview without <c>orderLine</c> is unusable and the
        /// verification fails open rather than guessing.
        /// </summary>
        internal sealed class TOFulfillmentTransformLine
        {
            [JsonPropertyName("orderLine")]
            [JsonConverter(typeof(LenientInt32Converter))]
            public int? OrderLine { get; set; }

            [JsonPropertyName("line")]
            [JsonConverter(typeof(LenientInt32Converter))]
            public int? Line { get; set; }

            [JsonPropertyName("quantity")]
            public decimal? Quantity { get; set; }

            [JsonPropertyName("inventoryDetail")]
            public JsonElement? InventoryDetail { get; set; }

            /// <summary>
            /// True when the preview already carries a non-empty inventory assignment.
            /// Under <c>?replace=item.inventoryDetail.inventoryAssignment</c>, replacing
            /// that with an EMPTY list removes sublist lines, which NetSuite rejects with
            /// "You have attempted an invalid sublist or line item operation".
            /// </summary>
            public bool HasInventoryAssignment =>
                InventoryDetail is { ValueKind: JsonValueKind.Object } detail
                && detail.TryGetProperty("inventoryAssignment", out var assignment)
                && assignment.ValueKind == JsonValueKind.Object
                && assignment.TryGetProperty("items", out var items)
                && items.ValueKind == JsonValueKind.Array
                && items.GetArrayLength() > 0;
        }

        private sealed class TOFulfillmentTransformPreview
        {
            [JsonPropertyName("item")]
            public TransformItemSublist? Item { get; set; }
        }

        private sealed class TransformItemSublist
        {
            [JsonPropertyName("items")]
            public List<TOFulfillmentTransformLine> Items { get; set; } = [];
        }

        /// <summary>
        /// NetSuite caps a SuiteQL response at 1,000 rows. The fresh read must be
        /// paged before the line SET can be trusted: an unpaged read makes every
        /// line beyond row 1,000 look closed, and the old code stripped those
        /// lines from the payload without a message (issue 1 in the resolution
        /// plan, 2026-10-23). Page size and runaway guard mirror
        /// <c>MGetTransferOrderLineQryHandler</c>.
        /// </summary>
        private const int TOFulfillmentLinePageSize = 1000;

        private const int TOFulfillmentLineMaxPages = 100;

        /// <summary>
        /// Re-reads the current SHIPPING-line state of one transfer order straight
        /// from NetSuite. Mirrors the line semantics of the packing load script
        /// (<c>NS_TO_x_Packing_Get_Items</c>): SHIPPING-type lines only, raw
        /// un-normalized quantities, keyed by the transaction line id the WMS
        /// carries as <c>LineSequenceNumber</c>, and deliberately free of UoM,
        /// bin, availability, vendor, status and category predicates - those are
        /// display concerns and must never remove a line the transform exposes.
        /// </summary>
        private async Task<IReadOnlyCollection<TOFulfillmentFreshLineState>> GetFreshTOFulfillmentLineStateAsync(int orderId)
        {
            if (orderId <= 0)
            {
                throw new InvalidOperationException("A transfer-order internal id is required to read its fulfillment line state.");
            }

            // Constant integer interpolation (no user text) - injection-safe.
            // ORDER BY makes consecutive pages neither repeat nor skip a line.
            var query = $"""
                SELECT
                    tl.id AS LineId,
                    tl.item AS Item,
                    i.itemid AS ItemCode,
                    i.displayname AS ItemName,
                    ABS(tl.quantity) AS LineQuantity,
                    NVL(tl.quantityshiprecv, 0) AS QuantityShipped,
                    NVL(tl.quantitypacked, 0) AS QuantityPacked,
                    NVL(tl.quantitybackordered, 0) AS QuantityBackOrdered,
                    tl.isclosed AS IsClosed,
                    NVL(u.conversionrate, 1) AS UoMRate
                FROM transactionline tl
                JOIN transaction t ON tl.transaction = t.id
                JOIN item i ON i.id = tl.item
                LEFT JOIN unitstypeuom u ON u.internalid = tl.units
                WHERE t.id = {orderId}
                    AND t.recordtype IN ('transferorder', 'intercompanytransferorder')
                    AND tl.transactionlinetype = 'SHIPPING'
                ORDER BY tl.id
                """;

            var rows = await ReadAllSuiteQLPagesAsync(
                (limit, offset) => ExecuteSuiteQLQuery<TOFulfillmentFreshLineState>(query, limit, offset),
                TOFulfillmentLinePageSize,
                TOFulfillmentLineMaxPages,
                $"TO fulfillment line read for order {orderId}");

            return rows;
        }

        /// <summary>
        /// Reads every page of a SuiteQL result with the limit/offset +
        /// <c>totalResults</c> contract used by the packing line read. A short or
        /// empty page is a valid terminator only when it accounts for the total
        /// NetSuite reported; otherwise the read throws instead of returning a
        /// partial list (the silent-truncation defect in a different shape).
        /// Extracted so the paging itself is unit-testable without HTTP.
        /// </summary>
        internal static async Task<List<T>> ReadAllSuiteQLPagesAsync<T>(
            Func<int, int, Task<NetSuiteResponse<T>>> fetchPage,
            int pageSize,
            int maxPages,
            string context)
        {
            var rows = new List<T>();
            int? expectedTotal = null;

            for (var page = 0; page < maxPages; page++)
            {
                var offset = page * pageSize;

                var response = await fetchPage(pageSize, offset);

                var batch = response?.items;
                if (batch is null || batch.Count == 0)
                {
                    break;
                }

                rows.AddRange(batch);

                if (response!.totalResults > 0)
                {
                    expectedTotal = response.totalResults;
                }

                // totalResults is authoritative, so a 1,000-row document stops on
                // its last full page instead of probing an offset NetSuite answers
                // with 404 INVALID_PARAMETER.
                if (response!.totalResults > 0 && rows.Count >= response.totalResults)
                {
                    break;
                }

                // hasMore is the page contract: a full batch does NOT prove
                // another page exists. An exact multiple with totalResults
                // absent otherwise probes a past-end offset, which NetSuite
                // answers with a 404 (SuiteQL-Shape-Rules Rule 4).
                if (!response.hasMore)
                {
                    break;
                }

                if (batch.Count < pageSize)
                {
                    break;
                }

                if (page == maxPages - 1)
                {
                    throw new InvalidOperationException(
                        $"{context} exceeded {maxPages} pages ({maxPages * pageSize:N0} rows). Refusing to return a partial list.");
                }
            }

            // A silently truncated set is indistinguishable from "these lines are
            // closed", which is exactly how a scanned line beyond the 1,000-row cap
            // used to disappear. Fail before posting instead.
            if (expectedTotal is > 0 && rows.Count < expectedTotal)
            {
                throw new InvalidOperationException(
                    $"{context} returned {rows.Count:N0} of {expectedTotal:N0} rows. Refusing to return a partial list.");
            }

            return rows;
        }

        /// <summary>
        /// Reads the item sublist of the <c>!transform/itemFulfillment</c> preview: the
        /// exact line set the POST validates, with the pre-filled quantity and inventory
        /// detail per line. Read-only (GET) - it creates nothing.
        /// </summary>
        private async Task<IReadOnlyCollection<TOFulfillmentTransformLine>> GetTOFulfillmentTransformLinesAsync(int orderId)
        {
            var url = string.Format(ItemFulfillmentUrlNoReplace, "transferOrder", orderId);
            var preview = await MakeRequest<TOFulfillmentTransformPreview>(url, null, HttpMethod.Get);
            return preview?.Item?.Items ?? [];
        }

        /// <summary>
        /// Result of checking the reconciled line set against the transform preview.
        /// </summary>
        internal sealed class TOFulfillmentTransformVerification
        {
            /// <summary>False when the preview could not be used (fail-open).</summary>
            public bool Available { get; init; }
            public string? UnavailableReason { get; init; }
            public List<PostTransferOrderDTO> Lines { get; init; } = [];
            public List<string> RejectedScans { get; init; } = [];
            public List<int> DroppedFillerIds { get; init; } = [];

            /// <summary>Fillers whose preview line already carries an inventory assignment.</summary>
            public List<int> FillersWithInventoryDetail { get; init; } = [];
            public int TransformLineCount { get; init; }

            /// <summary>Set when the preview exposes fewer lines than the open-line read.</summary>
            public string? BlockingError { get; init; }
        }

        /// <summary>
        /// Refuses a REST-transform payload larger than the measured ceiling. The message
        /// is the operator's instruction, so it names both sizes and the two working paths.
        /// </summary>
        internal static void EnsureTOFulfillmentTransformSize(int lineCount, int maxLines, string orderNumber)
        {
            if (lineCount <= maxLines)
            {
                return;
            }

            throw new Exception(
                $"Cannot post TO Item Fulfillment for order {orderNumber}: the payload would address {lineCount:N0} line(s) and the NetSuite transform accepts up to {maxLines:N0} per document "
                + "(larger payloads are rejected with \"invalid sublist or line item operation\"). Nothing was saved. "
                + "Fulfil this order in NetSuite, or enable WMS_TO_IF_USE_RESTLET once script 2522 has been piloted.");
        }

        /// <summary>
        /// Checks the payload's line SET against the transform preview instead of against the
        /// SuiteQL open-line read. The read is an approximation: a line it calls open that the
        /// transform does not expose fails the whole POST with "You have attempted an invalid
        /// sublist or line item operation", and every previous guard was blind to it because
        /// they all measured against that same read.
        ///
        /// Rules:
        /// <list type="bullet">
        /// <item>A preview with no usable <c>orderLine</c> ids is unusable - fail open, never guess.</item>
        /// <item>A preview exposing FEWER lines than the read expects blocks the save: dropping
        /// those lines would auto-fulfil them at their pre-filled quantity.</item>
        /// <item>A filler the preview does not expose is dropped - it cannot be addressed, and
        /// dropping it cannot auto-fulfil anything (a line absent from the sublist is not on
        /// the document at all).</item>
        /// <item>A scan the preview does not expose is rejected, naming the WMS line.</item>
        /// </list>
        /// </summary>
        internal static TOFulfillmentTransformVerification VerifyTOFulfillmentAgainstTransform(
            IReadOnlyCollection<PostTransferOrderDTO> lines,
            IReadOnlyCollection<TOFulfillmentTransformLine> transformLines,
            int expectedOpenLineCount)
        {
            if (transformLines.Count == 0)
            {
                return new()
                {
                    Available = false,
                    UnavailableReason = "the transform preview returned no item lines"
                };
            }

            var exposed = transformLines
                .Where(x => x.OrderLine is > 0)
                .GroupBy(x => x.OrderLine!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            if (exposed.Count == 0)
            {
                return new()
                {
                    Available = false,
                    UnavailableReason = "the transform preview carried no orderLine ids"
                };
            }

            var expected = Math.Max(
                expectedOpenLineCount,
                lines.Select(x => x.LineSequenceNumber).Distinct().Count());

            if (exposed.Count < expected)
            {
                var absent = lines
                    .Select(x => x.LineSequenceNumber)
                    .Distinct()
                    .Where(id => !exposed.ContainsKey(id))
                    .OrderBy(id => id)
                    .Take(10)
                    .ToList();

                return new()
                {
                    Available = true,
                    TransformLineCount = exposed.Count,
                    BlockingError =
                        $"the item fulfillment transform exposes {exposed.Count:N0} line(s) but the open-line read expects {expected:N0}, "
                        + $"and addressing the difference would auto-fulfil those lines at their pre-filled quantity. "
                        + $"Sample unexposed WMS lines: [{string.Join(",", absent)}]. Nothing was saved - fulfil this order in NetSuite, then report this message."
                };
            }

            var kept = new List<PostTransferOrderDTO>();
            var rejectedScans = new List<string>();
            var droppedFillerIds = new List<int>();
            var fillersWithInventoryDetail = new List<int>();

            foreach (var line in lines)
            {
                if (!exposed.TryGetValue(line.LineSequenceNumber, out var transformLine))
                {
                    if (line.ScannedQuantity > 0)
                    {
                        rejectedScans.Add(
                            $"WMS line {line.LineSequenceNumber} ({line.MaterialCode} - {line.MaterialName}): scanned {line.ScannedQuantity}, but the line is not exposed by the item fulfillment transform");
                    }
                    else
                    {
                        droppedFillerIds.Add(line.LineSequenceNumber);
                    }

                    continue;
                }

                // Diagnostic only: an empty ?replace= on a line that already carries an
                // assignment removes sublist lines. Reported so a field failure names it.
                if (line.ScannedQuantity == 0 && transformLine.HasInventoryAssignment)
                {
                    fillersWithInventoryDetail.Add(line.LineSequenceNumber);
                }

                // Quantity cap: the preview's own quantity is the amount NetSuite
                // would fulfil if the line went unaddressed - the exact maximum a
                // scan may claim, and the authoritative answer to the unit question
                // the SuiteQL read can only approximate (ScannedQuantity is in the
                // line's display unit; the preview quantity is the POST's quantity
                // unit, the same line UoM).
                if (line.ScannedQuantity > 0
                    && transformLine.Quantity is { } previewed
                    && line.ScannedQuantity > previewed + ScanQuantityTolerance)
                {
                    rejectedScans.Add(
                        $"WMS line {line.LineSequenceNumber} ({line.MaterialCode} - {line.MaterialName}): scanned {line.ScannedQuantity}, but the item fulfillment transform exposes only {previewed}");
                    continue;
                }

                kept.Add(line);
            }

            return new()
            {
                Available = true,
                TransformLineCount = exposed.Count,
                Lines = kept,
                RejectedScans = rejectedScans,
                DroppedFillerIds = droppedFillerIds,
                FillersWithInventoryDetail = fillersWithInventoryDetail
            };
        }

        /// <summary>
        /// Reconciliation switch (deployment rollback: set to false to restore the
        /// pre-reconciliation snapshot behaviour). Environment configuration, like
        /// the NetSuite credentials this service already reads.
        /// </summary>
        internal const string ReconciliationFlagVariable = "WMS_TO_IF_ENABLE_RECONCILIATION";

        /// <summary>
        /// Fail-closed switch for the authoritative read (D5). Off by default so
        /// the first deploy can be observed; a read failure still fails the save
        /// when this is on.
        /// </summary>
        internal const string FailClosedRevalidationFlagVariable = "WMS_TO_IF_ENABLE_FAIL_CLOSED_REVALIDATION";

        /// <summary>
        /// Transform-preview verification switch. The payload's line SET is checked
        /// against the <c>!transform/itemFulfillment</c> preview - the same body the
        /// POST validates - instead of trusting the SuiteQL open-line read.
        ///
        /// OFF BY DEFAULT: the preview cannot be read. NetSuite answers
        /// <c>GET .../!transform/itemFulfillment</c> with
        /// <c>405 INVALID_METHOD - Unsupported HTTP method</c> (field-verified
        /// 2026-10-23, order ISTR-THM00100000837/223191), because the transform action
        /// is POST-only. The reader and its guards stay for the day a supported preview
        /// exists (or a transient-POST preview is proven safe); enabling this flag
        /// without one only costs a failed call per save.
        /// </summary>
        internal const string TransformVerificationFlagVariable = "WMS_TO_IF_ENABLE_TRANSFORM_VERIFY";

        public static bool TransformVerificationEnabled =>
            ReadFlag(Environment.GetEnvironmentVariable(TransformVerificationFlagVariable), defaultValue: false);

        /// <summary>
        /// Upper bound on the entries one REST <c>!transform/itemFulfillment</c> POST may
        /// carry. The transform addresses the item sublist as a whole: field orders of
        /// 1,022 and 1,584 lines were rejected with "invalid sublist or line item
        /// operation" while small orders post. NetSuite's REST sublist boundary is 1,000,
        /// so anything above it is refused before the POST instead of after a doomed save.
        /// Override with <see cref="MaxTransformLinesVariable"/> once the real ceiling is
        /// measured on this account.
        /// </summary>
        internal const string MaxTransformLinesVariable = "WMS_TO_IF_MAX_TRANSFORM_LINES";

        public static int MaxTransformLines
        {
            get
            {
                var raw = Environment.GetEnvironmentVariable(MaxTransformLinesVariable);
                return int.TryParse(raw, out var value) && value > 0 ? value : 1000;
            }
        }

        /// <summary>
        /// Routes TO item fulfillment through the RESTlet (script 2522) instead of the REST
        /// <c>!transform/itemFulfillment</c> endpoint. OFF by default: the RESTlet sends only
        /// the scanned lines and relies on the SuiteScript to leave every other line
        /// unfulfilled, so it must be piloted on a partially scanned order first.
        /// </summary>
        internal const string UseRestletForTOIFVariable = "WMS_TO_IF_USE_RESTLET";

        public static bool UseRestletForTOFulfillment =>
            ReadFlag(Environment.GetEnvironmentVariable(UseRestletForTOIFVariable), defaultValue: false);

        public static bool ReconciliationEnabled =>
            ReadFlag(Environment.GetEnvironmentVariable(ReconciliationFlagVariable), defaultValue: true);

        public static bool FailClosedRevalidationEnabled =>
            ReadFlag(Environment.GetEnvironmentVariable(FailClosedRevalidationFlagVariable), defaultValue: false);

        internal static bool ReadFlag(string? value, bool defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            return value.Trim().ToLowerInvariant() switch
            {
                "1" or "true" or "yes" or "on" => true,
                "0" or "false" or "no" or "off" => false,
                _ => defaultValue
            };
        }

        /// <summary>
        /// Result of reconciling the submitted WMS rows against the authoritative
        /// open-line set: the completed payload input (scans plus zero-quantity
        /// fillers for every other open line), the scans that can no longer be
        /// fulfilled, and the counts needed by the save diagnostic.
        /// </summary>
        internal sealed class TOFulfillmentReconciliation
        {
            public List<PostTransferOrderDTO> Lines { get; init; } = [];
            public List<string> RejectedScans { get; init; } = [];
            public int OpenLineCount { get; init; }
            public int FillerCount { get; init; }
        }

        /// <summary>
        /// Builds the complete payload input from the authoritative open-line set
        /// (D1/D6): every submitted scan that is still open survives with fresh
        /// quantity state, and every other open line becomes a zero-quantity
        /// filler so the transform's pre-filled item sublist is addressed on every
        /// line. The mobile vendor gate stays a work-scoping control only.
        /// </summary>
        /// <remarks>
        /// Only scanned submitted rows are required to be open. A submitted
        /// zero-quantity row is a stale client-side filler: it is replaced by the
        /// authoritative filler (or dropped when the line closed), never treated
        /// as an operator conflict.
        /// </remarks>
        internal static TOFulfillmentReconciliation ReconcileTOFulfillmentLines(
            IReadOnlyCollection<PostTransferOrderDTO> submitted,
            IReadOnlyCollection<TOFulfillmentFreshLineState> freshLines,
            int orderId,
            bool isUsedBin)
        {
            var openById = freshLines
                .Where(x => x.IsOpen)
                .GroupBy(x => x.LineId)
                .ToDictionary(g => g.Key, g => g.First());

            var completed = new List<PostTransferOrderDTO>();
            var rejectedScans = new List<string>();
            var covered = new HashSet<int>();

            foreach (var line in submitted.Where(x => x.ScannedQuantity > 0))
            {
                if (!openById.TryGetValue(line.LineSequenceNumber, out var fresh))
                {
                    rejectedScans.Add(
                        $"WMS line {line.LineSequenceNumber} ({line.MaterialCode} - {line.MaterialName}): scanned {line.ScannedQuantity}, but the line is no longer open on the order");
                    continue;
                }

                line.LineQuantity = fresh.LineQuantity;
                line.QuantityAlreadyShipped = fresh.QuantityShipped;
                line.LineQuantityPacked = fresh.QuantityPacked;
                line.LineQuantityBackOrdered = fresh.QuantityBackOrdered;
                line.IsClosed = fresh.IsClosed;
                // The save diagnostic reports the rate the save actually ran on,
                // not the device's load-time value (2b will use it in the guards).
                line.UoMRate = fresh.UoMRate;
                if (string.IsNullOrWhiteSpace(line.MaterialCode)) line.MaterialCode = fresh.ItemCode;
                if (string.IsNullOrWhiteSpace(line.MaterialName)) line.MaterialName = fresh.ItemName;
                if (line.NetsuiteMaterialInternalId == 0) line.NetsuiteMaterialInternalId = fresh.Item;

                completed.Add(line);
                covered.Add(line.LineSequenceNumber);
            }

            var fillerCount = 0;

            foreach (var fresh in openById.Values.OrderBy(x => x.LineId))
            {
                if (covered.Contains(fresh.LineId))
                {
                    continue;
                }

                completed.Add(new PostTransferOrderDTO
                {
                    NetsuiteOrderInternalId = orderId,
                    LineSequenceNumber = fresh.LineId,
                    NetsuiteMaterialInternalId = fresh.Item,
                    MaterialCode = fresh.ItemCode,
                    MaterialName = fresh.ItemName,
                    LineQuantity = fresh.LineQuantity,
                    QuantityAlreadyShipped = fresh.QuantityShipped,
                    LineQuantityPacked = fresh.QuantityPacked,
                    LineQuantityBackOrdered = fresh.QuantityBackOrdered,
                    IsClosed = fresh.IsClosed,
                    UoMRate = fresh.UoMRate,
                    ScannedQuantity = 0,
                    IsBad = false,
                    IsLocationUsedBin = isUsedBin
                });
                fillerCount++;
            }

            return new TOFulfillmentReconciliation
            {
                Lines = completed,
                RejectedScans = rejectedScans,
                OpenLineCount = openById.Count,
                FillerCount = fillerCount
            };
        }

        /// <summary>
        /// Lines whose whole remaining quantity is bad-scanned. The BAD post runs
        /// first and closes them, so the GOOD transform (created afterwards) no
        /// longer exposes them: a filler referencing a closed line is an invalid
        /// insert on a static sublist and would 400 the whole GOOD post.
        /// The comparison uses the display-unit packable quantity - ScannedQuantity
        /// is in display units while RemainingQuantity is raw stock units, and
        /// comparing the two under-counted closure on multi-unit lines.
        /// </summary>
        private static HashSet<int> LinesClosedByBadPost(
            IReadOnlyCollection<PostTransferOrderDTO> completed)
        {
            return completed
                .Where(x => x.IsBad)
                .GroupBy(x => x.LineSequenceNumber)
                .Where(g => g.Sum(x => x.ScannedQuantity) >= PackableDisplayQuantity(g.First()) - ScanQuantityTolerance)
                .Select(g => g.Key)
                .ToHashSet();
        }

        /// <summary>
        /// The orderLine ids one quality bucket must address: the lines it ships
        /// itself, plus every other open line - which it must address at quantity
        /// zero, otherwise the transform fulfils it at its pre-filled quantity.
        /// </summary>
        internal static List<int> ExpectedTOFulfillmentBucketLineIds(
            IReadOnlyCollection<PostTransferOrderDTO> completed,
            bool isBadBucket)
        {
            var ownIds = completed
                .Where(x => x.IsBad == isBadBucket)
                .Select(x => x.LineSequenceNumber)
                .ToHashSet();
            var closedByBadPost = isBadBucket ? [] : LinesClosedByBadPost(completed);

            return completed
                .Select(x => x.LineSequenceNumber)
                .Where(id => ownIds.Contains(id) || !closedByBadPost.Contains(id))
                .Distinct()
                .OrderBy(id => id)
                .ToList();
        }

        /// <summary>
        /// Builds the postable input for one quality bucket (D6): the bucket's own
        /// rows at their scanned quantity, plus a zero-quantity filler for every
        /// other open line. Reconciliation runs once; only the split is per bucket.
        /// </summary>
        internal static List<PostTransferOrderDTO> BuildTOFulfillmentBucketLines(
            IReadOnlyCollection<PostTransferOrderDTO> completed,
            bool isBadBucket)
        {
            var own = completed.Where(x => x.IsBad == isBadBucket).ToList();
            var ownIds = own.Select(x => x.LineSequenceNumber).ToHashSet();
            var closedByBadPost = isBadBucket ? [] : LinesClosedByBadPost(completed);

            var fillers = completed
                .Where(x => !ownIds.Contains(x.LineSequenceNumber))
                .Where(x => !closedByBadPost.Contains(x.LineSequenceNumber))
                .GroupBy(x => x.LineSequenceNumber)
                .Select(g => ToZeroQuantityFiller(g.First(), isBadBucket))
                .ToList();

            return [.. own, .. fillers];
        }

        /// <summary>
        /// A filler clone: same line identity, nothing shipped. Cloned rather than
        /// mutated because the same line may be a scan in the other bucket.
        /// </summary>
        private static PostTransferOrderDTO ToZeroQuantityFiller(PostTransferOrderDTO source, bool bucketIsBad) => new()
        {
            NetsuiteOrderInternalId = source.NetsuiteOrderInternalId,
            OrderNumber = source.OrderNumber,
            OrderType = source.OrderType,
            OrderStatus = source.OrderStatus,
            LocationName = source.LocationName,
            LocationUsedBin = source.LocationUsedBin,
            IsLocationUsedBin = source.IsLocationUsedBin,
            LineSequenceNumber = source.LineSequenceNumber,
            TransactionLineType = source.TransactionLineType,
            NetsuiteMaterialInternalId = source.NetsuiteMaterialInternalId,
            MaterialCode = source.MaterialCode,
            MaterialName = source.MaterialName,
            NetsuiteMaterialPrefferedBinId = source.NetsuiteMaterialPrefferedBinId,
            NetsuiteMaterialVendorAssignedBin = source.NetsuiteMaterialVendorAssignedBin,
            LineQuantity = source.LineQuantity,
            LineQuantityPacked = source.LineQuantityPacked,
            LineQuantityBackOrdered = source.LineQuantityBackOrdered,
            QuantityAlreadyShipped = source.QuantityAlreadyShipped,
            IsClosed = source.IsClosed,
            NetsuiteUoMInternalId = source.NetsuiteUoMInternalId,
            UoMName = source.UoMName,
            UoMRate = source.UoMRate,
            ScannedQuantity = 0,
            ScannedWeight = 0,
            ScanCount = 0,
            IsBad = bucketIsBad,
            IsTripTicketExempt = source.IsTripTicketExempt,
            TransferCategory = source.TransferCategory
        };

        /// <summary>
        /// F2 pre-flight assertion: the bucket addresses exactly the open set it is
        /// responsible for, exactly once. This is the guard that makes the
        /// "line never addressed" defect (RCA issue 1) unable to come back.
        /// </summary>
        internal static void EnsureTOFulfillmentBucketIsComplete(
            string bucket,
            IReadOnlyCollection<PostTransferOrderDTO> bucketLines,
            IReadOnlyCollection<PostTransferOrderDTO> completedLines,
            bool isBadBucket)
        {
            var postedIds = bucketLines.Select(x => x.LineSequenceNumber).ToList();
            var duplicates = postedIds.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            var expected = ExpectedTOFulfillmentBucketLineIds(completedLines, isBadBucket);
            var missing = expected.Except(postedIds).ToList();
            var extra = postedIds.Distinct().Except(expected).ToList();

            if (duplicates.Count == 0 && missing.Count == 0 && extra.Count == 0)
            {
                return;
            }

            throw new Exception(
                $"Cannot post {bucket} TO Item Fulfillment: internal line-set guard failed "
                + $"(duplicate=[{string.Join(",", duplicates)}] missing=[{string.Join(",", missing)}] extra=[{string.Join(",", extra)}]). "
                + "Nothing was saved. Reload the order and retry; report this message if it repeats.");
        }

        /// <summary>
        /// Merges freshly-read NetSuite line state into the posted DTO rows. The
        /// guards downstream of this merge (closed-line strip, over-remaining
        /// check, missing-bin check) keep their existing logic and run on CURRENT
        /// state instead of the device's load-time snapshot - the snapshot can be
        /// hours old on a 700-line order, and a line fulfilled elsewhere in that
        /// window is absent from the transform's item sublist, which is the
        /// "invalid sublist or line item operation" 400 (field report 2026-10-21).
        /// Lines no longer present on the order are marked closed, which the
        /// existing strip already removes from the payload.
        /// </summary>
        /// <returns>Descriptors for scanned lines that were closed or fully
        /// shipped since the snapshot - quantities the operator physically has in
        /// hand but that can no longer be fulfilled on this order.</returns>
        internal static List<string> MergeFreshTOFulfillmentLineState(
            List<PostTransferOrderDTO> lines,
            IReadOnlyCollection<TOFulfillmentFreshLineState> freshLines)
        {
            var freshById = freshLines.ToDictionary(x => x.LineId);
            var newlyClosedScans = new List<string>();

            foreach (var line in lines)
            {
                if (!freshById.TryGetValue(line.LineSequenceNumber, out var fresh))
                {
                    // Absent from the order's current SHIPPING lines: the same
                    // condition NetSuite reports as an invalid insert on a static
                    // sublist. Mark closed so the existing strip drops it.
                    if (!line.IsLineClosed) line.IsClosed = "T";
                    continue;
                }

                line.LineQuantity = fresh.LineQuantity;
                line.QuantityAlreadyShipped = fresh.QuantityShipped;
                line.LineQuantityPacked = fresh.QuantityPacked;
                line.LineQuantityBackOrdered = fresh.QuantityBackOrdered;
                line.IsClosed = fresh.IsClosed;

                if (line.ScannedQuantity > 0 && (line.IsLineClosed || line.RemainingQuantity <= 0))
                {
                    newlyClosedScans.Add(
                        $"WMS line {line.LineSequenceNumber} ({line.MaterialCode} - {line.MaterialName}): scanned {line.ScannedQuantity}, already shipped {fresh.QuantityShipped} of {fresh.LineQuantity}");
                }
            }

            return newlyClosedScans;
        }

        /// <summary>
        /// Decodes a transfer-order itemFulfillment rejection into the offending DTO row so a
        /// field failure names the item without a server-logs round trip. NetSuite emits two
        /// phrasings: "line N of the item list" (an inventoryDetail without a bin, where N is
        /// the position in the payload's item sublist) and "Please configure the inventory
        /// detail for &lt;item name&gt;." (no position). The second is resolved by MaterialName
        /// / MaterialCode; the item name may itself contain '.', so the capture runs to the end
        /// of the message and is trimmed of the trailing sentence period rather than stopping
        /// at the first dot.
        /// </summary>
        /// <remarks>
        /// NetSuite names the item from ITS record while the payload carries only orderLine, so
        /// WMS's MaterialName can differ from the reported name by nothing more than whitespace
        /// (field report 2026-10-21: "BONAKID 3/400G(1.2KG)" reported as not posted). Comparison
        /// is therefore whitespace- and case-insensitive. When no exact match exists the decoder
        /// still must locate the line without server logs, so it reports up to three entries whose
        /// name/code share the reported base name (the part before the first parenthesis) - the
        /// filler entries a large open-set payload carries are indistinguishable from scans
        /// otherwise. Reporting a count instead of inventing a match, plus the posted orderLine
        /// ids, keeps the "is not in the submitted set" wording truthful when the item really is
        /// absent.
        /// </remarks>
        internal static string DecodeTOItemFulfillmentLine(string message, List<PostTransferOrderDTO> lines)
        {
            var nsLineMatch = Regex.Match(message, @"line (\d+) of the item list");
            if (nsLineMatch.Success && int.TryParse(nsLineMatch.Groups[1].Value, out var nsLine) && nsLine >= 1 && nsLine <= lines.Count)
            {
                var offender = lines[nsLine - 1];
                return $" | line{nsLine}=WMSLine {offender.LineSequenceNumber} {offender.MaterialCode} - {offender.MaterialName} vendorBin={offender.NetsuiteMaterialVendorAssignedBin} preferredBin={offender.NetsuiteMaterialPrefferedBinId} qty={offender.ScannedQuantity}";
            }

            var itemMatch = Regex.Match(message, @"inventory detail for (.+?)\s*$");
            if (!itemMatch.Success) return string.Empty;

            var itemName = itemMatch.Groups[1].Value.TrimEnd('.', ' ');
            var key = NormalizeDiagnosticName(itemName);

            var matched = lines.FirstOrDefault(x =>
                NormalizeDiagnosticName(x.MaterialName) == key ||
                NormalizeDiagnosticName(x.MaterialCode) == key);

            if (matched is not null)
            {
                return $" | item={matched.MaterialCode} - {matched.MaterialName} WMSLine {matched.LineSequenceNumber} vendorBin={matched.NetsuiteMaterialVendorAssignedBin} preferredBin={matched.NetsuiteMaterialPrefferedBinId} qty={matched.ScannedQuantity}";
            }

            // Base-name candidates: the reported name is often the same item with a suffix the
            // WMS master data does not carry ("3/400G(1.2KG)" vs "3/400G"), plus a pack suffix
            // ("90MLX2", "65G 6+1"). Capped at three so the toast stays readable on an order
            // that posted hundreds of fillers.
            var stem = Regex.Replace(
                key.Split('(')[0],
                @"(?:\d+\+\d+|X\d+)$",
                string.Empty);
            if (stem.Length < 3) stem = key;

            var candidates = lines
                .Select((line, index) => (line, position: index + 1))
                .Where(x =>
                {
                    var name = NormalizeDiagnosticName(x.line.MaterialName);
                    var code = NormalizeDiagnosticName(x.line.MaterialCode);
                    return (stem.Length > 0 && (name.StartsWith(stem) || code.StartsWith(stem) || name.Contains(stem)))
                        || (key.Contains(code) && code.Length >= 3);
                })
                .Take(3)
                .Select(x => $"item-list line {x.position} (WMS line {x.line.LineSequenceNumber}) {x.line.MaterialCode} qty={x.line.ScannedQuantity} vendorBin={x.line.NetsuiteMaterialVendorAssignedBin} preferredBin={x.line.NetsuiteMaterialPrefferedBinId}")
                .ToList();

            var candidateText = candidates.Count > 0
                ? $"candidates=[{string.Join("; ", candidates)}]"
                : $"candidates=[none among {lines.Count} posted entries]";

            // An absent item is a line the payload never addressed; the posted
            // orderLine ids are the authoritative statement of what WAS addressed
            // (D7 - never identify a line by its display name).
            var postedIds = string.Join(",", lines.Take(10).Select(x => x.LineSequenceNumber));

            return $" | item=\"{itemName}\" is not in the submitted set postedIds=[{postedIds}] {candidateText}";
        }

        /// <summary>
        /// Whitespace- and case-insensitive form of an item name or code, used only to match
        /// NetSuite's reported item name against the posted DTO rows.
        /// </summary>
        private static string NormalizeDiagnosticName(string? value) =>
            string.Concat((value ?? string.Empty).Where(c => !char.IsWhiteSpace(c))).ToUpperInvariant();

        /// <summary>
        /// One-line, length-capped form of a diagnostic reason. The toast is the only
        /// channel a field failure has, so a multi-line exception message must not
        /// break its layout; the API log keeps the untouched text.
        /// </summary>
        private static string SanitizeDiagnosticText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var single = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
            return single.Length <= 120 ? single : single[..120] + "…";
        }

        /// <summary>
        /// 1.4 - rejects a request that has nothing to fulfil or that mixes orders
        /// before the first NetSuite call. Extracted so both guards are testable
        /// without HTTP. Input rule 1 and 2 of the target contract.
        /// </summary>
        internal static void ValidateTOFulfillmentRequest(IReadOnlyCollection<PostTransferOrderDTO> lines)
        {
            if (!lines.Any(x => x.ScannedQuantity > 0))
            {
                throw new Exception("Cannot post TO Item Fulfillment: the request contains no positive scanned quantity. Reload the order, rescan the affected line(s), and try again. Nothing was saved.");
            }

            var orderIds = lines.Select(x => x.NetsuiteOrderInternalId).Distinct().ToList();
            if (orderIds.Count != 1 || orderIds[0] <= 0)
            {
                throw new Exception($"Cannot post TO Item Fulfillment: the request must identify exactly one transfer order, but found [{string.Join(", ", orderIds)}]. Reload the order and try again. Nothing was saved.");
            }
        }

        public async Task<bool> SaveTOItemFulfillment(List<PostTransferOrderDTO> Data)
        {
            if (UseRestletForTOFulfillment)
            {
                return await SaveTOItemFulfillmentViaRestlet(Data);
            }

            // Captured before the deterministic collapse: the diagnostic reports the
            // count, and a successful save logs the collapsed keys (input rule 4).
            var duplicateGroups = Data
                .GroupBy(x => (x.LineSequenceNumber, x.IsBad))
                .Where(g => g.Count() > 1)
                .ToList();
            var duplicateLineCount = duplicateGroups.Count;
            var duplicateKeys = duplicateGroups
                .Select(g => $"WMS{g.Key.LineSequenceNumber}/{(g.Key.IsBad ? "BAD" : "GOOD")}x{g.Count()}")
                .ToList();

            Data = DeduplicateFulfillmentLines(Data, x => x.LineSequenceNumber, x => x.IsBad, x => x.ScannedQuantity);

            ValidateTOFulfillmentRequest(Data);

            var orderId = Data[0].NetsuiteOrderInternalId;
            var orderNumber = Data.FirstOrDefault()?.OrderNumber ?? orderId.ToString();

            // Header bin flag from the SUBMITTED rows (D3/1.3): fillers inherit it,
            // so it must be decided before the authoritative set adds fillers.
            // Use Any (not First): one stale non-bin line must not downgrade a
            // bin-tracked fulfillment to the legacy no-detail shape.
            var isUsedBin = Data.Any(x => x.IsLocationUsedBin);
            var mixedBinFlags = Data.Select(x => x.IsLocationUsedBin).Distinct().Count() > 1;

            if (mixedBinFlags)
            {
                var flagSample = string.Join("; ", Data.Take(10).Select((line, i) => $"pos{i + 1}/WMS{line.LineSequenceNumber}/{line.MaterialCode} usedBin={line.IsLocationUsedBin} ({line.LocationUsedBin})"));
                throw new Exception($"Cannot post BAD TO Item Fulfillment: mixed bin-tracked flags in one save (location '{Data.FirstOrDefault()?.LocationName}'). Reload the order so every line resolves the same location, then retry. Sample: {flagSample}.");
            }

            var submittedLineCount = Data.Count;
            // -1 means "unknown": the authoritative read failed in fail-open mode,
            // so the diagnostic must not claim the order has zero open lines.
            var openLineCount = -1;
            var missingLineCount = 0;
            HashSet<int>? openLineIds = null;
            List<string>? newlyClosedScans = null;

            // D2 - read authoritative state immediately before posting, paged.
            // D5 - fail-open first: when the read itself fails and the fail-closed
            // flag is off, the save proceeds on the client snapshot exactly as
            // before, so the deploy adds no new availability failure. When the
            // flag is on the save stops: a line set nobody can establish must not
            // be posted.
            IReadOnlyCollection<TOFulfillmentFreshLineState>? freshLines = null;
            try
            {
                freshLines = await GetFreshTOFulfillmentLineStateAsync(orderId);
            }
            catch (Exception ex)
            {
                if (FailClosedRevalidationEnabled)
                {
                    throw new Exception($"Cannot post TO Item Fulfillment for order {orderNumber}: the current open-line state could not be read. Nothing was saved. {ex.Message}", ex);
                }

                logger.LogWarning(ex, "TO IF authoritative line read unavailable for order {OrderId}; continuing with the client's loaded snapshot (fail-open).", orderId);
            }

            if (freshLines is not null)
            {
                openLineIds = freshLines.Where(x => x.IsOpen).Select(x => x.LineId).ToHashSet();

                if (ReconciliationEnabled)
                {
                    var reconciled = ReconcileTOFulfillmentLines(Data, freshLines, orderId, isUsedBin);

                    // A scanned line that is no longer open must fail loud: the
                    // operator physically holds goods that can no longer ship on
                    // this order. A stale zero-quantity client filler is replaced
                    // (or dropped) silently by the reconciliation.
                    if (reconciled.RejectedScans.Count > 0)
                    {
                        throw new Exception($"Cannot post TO Item Fulfillment: the following scanned line(s) were fulfilled or closed after this order was loaded: {string.Join("; ", reconciled.RejectedScans)}. Reload the order and rescan the affected line(s).");
                    }

                    Data = reconciled.Lines;
                    openLineCount = reconciled.OpenLineCount;
                    missingLineCount = reconciled.FillerCount;
                }
                else
                {
                    // Rollback path (WMS_TO_IF_ENABLE_RECONCILIATION=false): the
                    // pre-reconciliation freshness merge, unchanged.
                    newlyClosedScans = MergeFreshTOFulfillmentLineState(Data, freshLines);
                    openLineCount = freshLines.Count(x => x.IsOpen);
                }
            }

            if (newlyClosedScans is { Count: > 0 })
            {
                throw new Exception($"Cannot post TO Item Fulfillment: the following scanned line(s) were fulfilled or closed after this order was loaded: {string.Join("; ", newlyClosedScans)}. Reload the order and rescan the affected line(s).");
            }

            // Partially fulfilled orders expose only their OPEN lines on the transform's
            // item sublist; a closed line has no sublist line to address and any orderLine
            // reference for it (scanned entry or zero-qty filler) is an invalid insert on a
            // static sublist that fails the entire save. Strip closed / fully-shipped lines
            // before bucketing so the payload builder only ever receives postable lines.
            Data = Data.Where(x => !IsClosedFulfillmentLine(x)).ToList();

            // Reject scans beyond the remaining open quantity before the first NetSuite
            // call: on a partially fulfilled order quantitypacked is not the open quantity,
            // so surface the WMS line and the real ordered/remaining figures to the operator
            // instead of a cryptic NetSuite 400 mid-post.
            EnsureScansWithinPackableQuantity(Data);

            // D2b - verify the line SET against the transform preview, the same body the POST
            // validates. The SuiteQL read is an approximation; a line it calls open that the
            // transform does not expose is an "invalid sublist or line item operation" 400 that
            // fails the entire save, and every earlier guard was blind to it because they all
            // measured against that same read (field report 2026-10-23, order 226006: the diag
            // reported extra=0 while NetSuite rejected the payload).
            var transformLineCount = -1;
            List<int> droppedFillerIds = [];
            List<int> fillersWithInventoryDetail = [];
            var transformVerifyError = string.Empty;

            if (TransformVerificationEnabled)
            {
                IReadOnlyCollection<TOFulfillmentTransformLine>? transformLines = null;
                try
                {
                    transformLines = await GetTOFulfillmentTransformLinesAsync(orderId);
                }
                catch (Exception ex)
                {
                    transformVerifyError = ex.Message;
                    logger.LogWarning(ex, "TO IF transform preview unavailable for order {OrderId}; posting on the SuiteQL line set (fail-open).", orderId);
                }

                if (transformLines is not null)
                {
                    var verification = VerifyTOFulfillmentAgainstTransform(Data, transformLines, openLineCount);

                    if (!verification.Available)
                    {
                        transformVerifyError = verification.UnavailableReason ?? string.Empty;
                        logger.LogWarning(
                            "TO IF transform preview unusable for order {OrderId}: {Reason}. Posting on the SuiteQL line set (fail-open).",
                            orderId,
                            verification.UnavailableReason);
                    }
                    else
                    {
                        if (verification.BlockingError is not null)
                        {
                            throw new Exception($"Cannot post TO Item Fulfillment for order {orderNumber}: {verification.BlockingError}");
                        }

                        if (verification.RejectedScans.Count > 0)
                        {
                            throw new Exception($"Cannot post TO Item Fulfillment: the following scanned line(s) are not exposed by the item fulfillment transform: {string.Join("; ", verification.RejectedScans)}. Reload the order and rescan the affected line(s).");
                        }

                        Data = verification.Lines;
                        transformLineCount = verification.TransformLineCount;
                        droppedFillerIds = verification.DroppedFillerIds;
                        fillersWithInventoryDetail = verification.FillersWithInventoryDetail;

                        if (droppedFillerIds.Count > 0)
                        {
                            logger.LogWarning(
                                "TO IF for order {OrderId}: dropped {Count} filler line(s) the transform does not expose: [{Ids}].",
                                orderId,
                                droppedFillerIds.Count,
                                string.Join(",", droppedFillerIds.Take(20)));
                        }

                        if (fillersWithInventoryDetail.Count > 0)
                        {
                            logger.LogWarning(
                                "TO IF for order {OrderId}: {Count} filler line(s) already carry an inventory assignment in the transform preview: [{Ids}]. An empty ?replace= on these removes sublist lines and NetSuite rejects the save with 'invalid sublist or line item operation'.",
                                orderId,
                                fillersWithInventoryDetail.Count,
                                string.Join(",", fillersWithInventoryDetail.Take(20)));
                        }
                    }
                }
            }

            var isTripTicketExempt = Data.FirstOrDefault()?.IsTripTicketExempt == true;
            var itemFulfillmentStatus = isTripTicketExempt ? "C" : "B";
            var printLabel = true; // Packing labels are required regardless of trip-ticket exemption.

            // The rest of the save builds the bucket payload from Data; its size IS the
            // number of entries NetSuite will see in item.items[]. Refuse a payload above
            // the measured transform ceiling before the POST, not after it fails.
            EnsureTOFulfillmentTransformSize(Data.Count, MaxTransformLines, orderNumber);

            // 1.5 - reconciliation ran once; only the split is per bucket. Each
            // bucket is completed with fillers for every open line it does not
            // own, otherwise the transform fulfils those lines at full quantity
            // (RCA issue 1 / D6).
            var badTO = BuildTOFulfillmentBucketLines(Data, isBadBucket: true);
            var goodTO = BuildTOFulfillmentBucketLines(Data, isBadBucket: false);

            if (badTO.Any(x => x.ScannedQuantity > 0))
            {
                EnsureTOFulfillmentBucketIsComplete("BAD", badTO, Data, isBadBucket: true);

                // NetSuite rejects inventoryDetail without binNumber at bin-tracked locations ("configure the inventory detail in line N").
                // Fail here, naming the offending items and their item-list positions, instead of surfacing a cryptic 400.
                var badMissingBin = new List<string>();
                for (var i = 0; i < badTO.Count; i++)
                {
                    var line = badTO[i];
                    if (RequiresBin(isUsedBin, line.ScannedQuantity, line.NetsuiteMaterialVendorAssignedBin, line.NetsuiteMaterialPrefferedBinId))
                    {
                        badMissingBin.Add($"item-list line {i + 1} (WMS line {line.LineSequenceNumber}) {line.MaterialCode} - {line.MaterialName}");
                    }
                }

                if (badMissingBin.Any())
                {
                    throw new Exception($"Cannot post BAD TO Item Fulfillment: location '{Data.FirstOrDefault()?.LocationName}' is bin-tracked but no bin is resolved for: {string.Join("; ", badMissingBin)}. Reload the order; if the bin is still missing, ask Master Data to set a Vendor Bin Assignment or a preferred bin for these items.");
                }

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
                    var diag = BuildSaveDiagnostic("BAD", url, badTO, ex.Message);
                    throw new Exception($"Error while posting BAD TO Item Fulfillment. {ex.Message} {diag}", ex);
                }
            }

            // Fillers make goodTO non-empty by construction, so the bucket guard
            // must be "ships something", not "has entries": posting a document
            // that ships nothing is noise in NetSuite.
            if (goodTO.Any(x => x.ScannedQuantity > 0))
            {
                EnsureTOFulfillmentBucketIsComplete("GOOD", goodTO, Data, isBadBucket: false);

                // NetSuite rejects inventoryDetail without binNumber at bin-tracked locations ("configure the inventory detail in line N").
                // Fail here, naming the offending items and their item-list positions, instead of surfacing a cryptic 400.
                var goodMissingBin = new List<string>();
                for (var i = 0; i < goodTO.Count; i++)
                {
                    var line = goodTO[i];
                    if (RequiresBin(isUsedBin, line.ScannedQuantity, line.NetsuiteMaterialVendorAssignedBin, line.NetsuiteMaterialPrefferedBinId))
                    {
                        goodMissingBin.Add($"item-list line {i + 1} (WMS line {line.LineSequenceNumber}) {line.MaterialCode} - {line.MaterialName}");
                    }
                }

                if (goodMissingBin.Any())
                {
                    throw new Exception($"Cannot post GOOD TO Item Fulfillment: location '{Data.FirstOrDefault()?.LocationName}' is bin-tracked but no bin is resolved for: {string.Join("; ", goodMissingBin)}. Reload the order; if the bin is still missing, ask Master Data to set a Vendor Bin Assignment or a preferred bin for these items.");
                }

                // The body always carries inventoryDetail at a bin-tracked location
                // (fillers included), so the URL must always request the replace.
                var url = string.Format(isUsedBin ? ItemFulfillmentUrl : ItemFulfillmentUrlNoReplace, "transferOrder", orderId);

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
                    var diag = BuildSaveDiagnostic("GOOD", url, goodTO, ex.Message);
                    throw new Exception($"Error while posting GOOD TO Item Fulfillment. {ex.Message} {diag}", ex);
                }
            }

            // Structured save diagnostic (5.2/D7). The toast keeps NetSuite's
            // message; this suffix makes the line set and the failed bucket
            // reconstructable without a server-logs round trip. Ids are truncated
            // so a 1,657-line payload cannot overflow the operator toast.
            string BuildSaveDiagnostic(string bucket, string url, List<PostTransferOrderDTO> bucketLines, string message)
            {
                var postedIds = string.Join(",", bucketLines.Take(10).Select(x => x.LineSequenceNumber));
                var uomRate = bucketLines.FirstOrDefault(x => x.ScannedQuantity > 0)?.UoMRate ?? 0;
                // -1 = the authoritative read failed, so "extra" is unknowable
                // rather than a measured zero (the old hardcoded 0 could never
                // fail the acceptance check it exists for).
                var extra = openLineIds is null
                    ? -1
                    : bucketLines.Count(x => !openLineIds.Contains(x.LineSequenceNumber));
                var diag = $"[diag bucket={bucket} replace={url.Contains("replace=")} usedBin={isUsedBin} order={orderNumber}/{orderId} open={openLineCount} submitted={submittedLineCount} posted={bucketLines.Count} scannedLines={bucketLines.Count(x => x.ScannedQuantity > 0)} missing={missingLineCount} extra={extra} dup={duplicateLineCount} uomRate={uomRate} transform={transformLineCount} transformError=[{SanitizeDiagnosticText(transformVerifyError)}] droppedFillers=[{string.Join(",", droppedFillerIds.Take(10))}] fillersWithDetail=[{string.Join(",", fillersWithInventoryDetail.Take(10))}] postedIds=[{postedIds}] build={BuildStamp}]";

                if (bucketLines.Count > 10)
                {
                    diag += $" | postedIdsTruncated=yes totalPostedIds={bucketLines.Count}";
                }

                return diag + DecodeTOItemFulfillmentLine(message, bucketLines);
            }

            // Input rule 4: a collapse is deterministic, but it must not be silent.
            if (duplicateKeys.Count > 0)
            {
                logger.LogWarning(
                    "TO IF request for order {OrderNumber}/{OrderId} contained {Count} duplicate (WMS line, bucket) row(s); collapsed deterministically: {Keys}",
                    orderNumber,
                    orderId,
                    duplicateKeys.Count,
                    string.Join("; ", duplicateKeys));
            }

            return true;
        }

        /// <summary>
        /// Posts a TO item fulfillment through the RESTlet (script 2522): only the scanned
        /// lines, with no zero-quantity fillers. Reconciliation and the save guards are the
        /// same as the REST path, so a scan on a line that closed after load still fails loud.
        /// </summary>
        public async Task<bool> SaveTOItemFulfillmentViaRestlet(List<PostTransferOrderDTO> Data)
        {
            var duplicateGroups = Data
                .GroupBy(x => (x.LineSequenceNumber, x.IsBad))
                .Where(g => g.Count() > 1)
                .ToList();

            Data = DeduplicateFulfillmentLines(Data, x => x.LineSequenceNumber, x => x.IsBad, x => x.ScannedQuantity);

            ValidateTOFulfillmentRequest(Data);

            var orderId = Data[0].NetsuiteOrderInternalId;
            var orderNumber = Data.FirstOrDefault()?.OrderNumber ?? orderId.ToString();

            var isUsedBin = Data.Any(x => x.IsLocationUsedBin);
            if (Data.Select(x => x.IsLocationUsedBin).Distinct().Count() > 1)
            {
                var flagSample = string.Join("; ", Data.Take(10).Select((line, i) => $"pos{i + 1}/WMS{line.LineSequenceNumber}/{line.MaterialCode} usedBin={line.IsLocationUsedBin} ({line.LocationUsedBin})"));
                throw new Exception($"Cannot post TO Item Fulfillment: mixed bin-tracked flags in one save (location '{Data.FirstOrDefault()?.LocationName}'). Reload the order so every line resolves the same location, then retry. Sample: {flagSample}.");
            }

            var submittedLineCount = Data.Count;

            // Same authoritative read and reconciliation as the REST path: a scan on a line
            // fulfilled or closed since load must stop the save, not reach the RESTlet.
            IReadOnlyCollection<TOFulfillmentFreshLineState>? freshLines = null;
            try
            {
                freshLines = await GetFreshTOFulfillmentLineStateAsync(orderId);
            }
            catch (Exception ex)
            {
                if (FailClosedRevalidationEnabled)
                {
                    throw new Exception($"Cannot post TO Item Fulfillment for order {orderNumber}: the current open-line state could not be read. Nothing was saved. {ex.Message}", ex);
                }

                logger.LogWarning(ex, "TO IF authoritative line read unavailable for order {OrderId}; continuing with the client's loaded snapshot (fail-open).", orderId);
            }

            List<string>? newlyClosedScans = null;

            if (freshLines is not null)
            {
                if (ReconciliationEnabled)
                {
                    var reconciled = ReconcileTOFulfillmentLines(Data, freshLines, orderId, isUsedBin);

                    if (reconciled.RejectedScans.Count > 0)
                    {
                        throw new Exception($"Cannot post TO Item Fulfillment: the following scanned line(s) were fulfilled or closed after this order was loaded: {string.Join("; ", reconciled.RejectedScans)}. Reload the order and rescan the affected line(s).");
                    }

                    Data = reconciled.Lines;
                }
                else
                {
                    newlyClosedScans = MergeFreshTOFulfillmentLineState(Data, freshLines);
                }
            }

            if (newlyClosedScans is { Count: > 0 })
            {
                throw new Exception($"Cannot post TO Item Fulfillment: the following scanned line(s) were fulfilled or closed after this order was loaded: {string.Join("; ", newlyClosedScans)}. Reload the order and rescan the affected line(s).");
            }

            Data = Data.Where(x => !IsClosedFulfillmentLine(x)).ToList();

            EnsureScansWithinPackableQuantity(Data);

            var scanned = Data.Where(x => x.ScannedQuantity > 0).ToList();
            if (scanned.Count == 0)
            {
                throw new Exception("Cannot post TO Item Fulfillment: the request contains no positive scanned quantity. Reload the order, rescan the affected line(s), and try again. Nothing was saved.");
            }

            var shipStatus = Data.FirstOrDefault()?.IsTripTicketExempt == true ? "C" : "B";
            var url = ItemFulfillmentRestletUrl;

            string? jsonString = null;
            try
            {
                var payload = TransferOrderIFRestletPayloadDTO.CreateForItemFulfillmentRestlet(scanned, orderId, shipStatus, isUsedBin);

                jsonString = JsonSerializer.Serialize(payload, JsonSerializerOption);

                var response = await MakeRequest<TransferOrderIFRestletResponseDTO>(url, jsonString, HttpMethod.Post);

                if (response is { Success: false })
                {
                    throw new Exception($"the RESTlet reported success=false (itemFulfillmentId={response.ItemFulfillmentId}, status={response.Status ?? "unknown"})");
                }

                // The RESTlet commits the document; naming it here is the only way the
                // save can be verified without opening NetSuite.
                logger.LogInformation(
                    "TO IF RESTlet created itemFulfillment {ItemFulfillmentId} ({Status}) for order {OrderNumber}/{OrderId} with {ScannedLines} scanned line(s).",
                    response?.ItemFulfillmentId ?? 0,
                    response?.Status ?? "unknown",
                    orderNumber,
                    orderId,
                    scanned.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "TO Item Fulfillment RESTlet failed. Url: {Url}. Payload: {Payload}", url, jsonString);

                var postedIds = string.Join(",", scanned.Take(10).Select(x => x.LineSequenceNumber));
                var uomRate = scanned.FirstOrDefault()?.UoMRate ?? 0;
                var diag = $"[diag transport=restlet order={orderNumber}/{orderId} submitted={submittedLineCount} scannedLines={scanned.Count} postedLines={scanned.Count} dup={duplicateGroups.Count} uomRate={uomRate} postedIds=[{postedIds}] build={BuildStamp}]";

                throw new Exception($"Error while posting TO Item Fulfillment (RESTlet). {ex.Message} {diag}", ex);
            }

            if (duplicateGroups.Count > 0)
            {
                logger.LogWarning(
                    "TO IF RESTlet request for order {OrderNumber}/{OrderId} contained {Count} duplicate (WMS line, bucket) row(s); collapsed deterministically.",
                    orderNumber,
                    orderId,
                    duplicateGroups.Count);
            }

            return true;
        }

        public async Task<bool> SaveReturnsItemFulfillment(List<PostReturnsDTO> Data)
        {
            Data = DeduplicateFulfillmentLines(Data, x => x.LineSequenceNumber, x => x.IsBad, x => x.ScannedQuantity);

            var orderId = Data.Select(x => x.NetsuiteOrderInternalId).FirstOrDefault();
            // Use Any (not First): one stale non-bin line must not downgrade a
            // bin-tracked fulfillment to the legacy no-detail shape.
            var isUsedBin = Data.Any(x => x.IsLocationUsedBin);
            var mixedBinFlags = Data.Select(x => x.IsLocationUsedBin).Distinct().Count() > 1;
            var isTripTicketExempt = Data.FirstOrDefault()?.IsTripTicketExempt == true;
            var itemFulfillmentStatus = isTripTicketExempt ? "C" : "B";
            var printLabel = true; // Packing labels are required regardless of trip-ticket exemption.

            // isUsedBin is an OR across lines, so one stale non-bin line would otherwise
            // decide the payload shape for the whole save and silently drop inventoryDetail
            // from every other line. Fail loud instead, matching SaveTOItemFulfillment.
            if (mixedBinFlags)
            {
                var flagSample = string.Join("; ", Data.Take(10).Select((line, i) => $"pos{i + 1}/WMS{line.LineSequenceNumber}/{line.MaterialCode} usedBin={line.IsLocationUsedBin} ({line.LocationUsedBin})"));
                throw new Exception($"Cannot post Returns Item Fulfillment: mixed bin-tracked flags in one save (location '{Data.FirstOrDefault()?.LocationName}'). Reload the order so every line resolves the same location, then retry. Sample: {flagSample}.");
            }

            // NetSuite rejects inventoryDetail without binNumber at bin-tracked locations ("configure the inventory detail in line N").
            // Fail here, naming the offending items and their item-list positions, instead of surfacing a cryptic 400.
            var returnsMissingBin = new List<string>();
            for (var i = 0; i < Data.Count; i++)
            {
                var line = Data[i];
                if (RequiresBin(isUsedBin, line.ScannedQuantity, line.NetsuiteMaterialVendorAssignedBin, line.NetsuiteMaterialPrefferedBinId))
                {
                    returnsMissingBin.Add($"item-list line {i + 1} (WMS line {line.LineSequenceNumber}) {line.MaterialCode} - {line.MaterialName}");
                }
            }

            if (returnsMissingBin.Any())
            {
                throw new Exception($"Cannot post Returns Item Fulfillment: location '{Data.FirstOrDefault()?.LocationName}' is bin-tracked but no bin is resolved for: {string.Join("; ", returnsMissingBin)}. Reload the order; if the bin is still missing, ask Master Data to set a Vendor Bin Assignment or a preferred bin for these items.");
            }

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
                var diag = $"[diag replace={url.Contains("replace=")} usedBin={isUsedBin} scannedLines={Data.Count(x => x.ScannedQuantity > 0)} totalLines={Data.Count} build={BuildStamp}]";

                // Decode NetSuite's "line N of the item list" into the actual DTO row so field tests identify the offender without server logs.
                var nsLineMatch = Regex.Match(ex.Message, @"line (\d+) of the item list");
                if (nsLineMatch.Success && int.TryParse(nsLineMatch.Groups[1].Value, out var nsLine) && nsLine >= 1 && nsLine <= Data.Count)
                {
                    var offender = Data[nsLine - 1];
                    diag += $" | line{nsLine}=WMSLine {offender.LineSequenceNumber} {offender.MaterialCode} - {offender.MaterialName} vendorBin={offender.NetsuiteMaterialVendorAssignedBin} preferredBin={offender.NetsuiteMaterialPrefferedBinId} qty={offender.ScannedQuantity}";
                }

                throw new Exception($"Error while posting Returns Item Fulfillment. {ex.Message} {diag}", ex);
            }

            return true;
        }

        public async Task<bool> SaveVRAItemFulfillment(List<PostVendorReturnAuthorizationDTO> Data)
        {
            Data = DeduplicateFulfillmentLines(Data, x => x.LineSequenceNumber, x => x.IsBad, x => x.ScannedQuantity);

            var orderId = Data.Select(x => x.NetsuiteOrderInternalId).FirstOrDefault();
            // Use Any (not First): one stale non-bin line must not downgrade a
            // bin-tracked fulfillment to the legacy no-detail shape.
            var isUsedBin = Data.Any(x => x.IsLocationUsedBin);
            var mixedBinFlags = Data.Select(x => x.IsLocationUsedBin).Distinct().Count() > 1;
            var isTripTicketExempt = Data.FirstOrDefault()?.IsTripTicketExempt == true;
            var itemFulfillmentStatus = isTripTicketExempt ? "C" : "B";
            var printLabel = true; // Packing labels are required regardless of trip-ticket exemption.

            // isUsedBin is an OR across lines, so one stale non-bin line would otherwise
            // decide the payload shape for the whole save and silently drop inventoryDetail
            // from every other line. Fail loud instead, matching SaveTOItemFulfillment.
            if (mixedBinFlags)
            {
                var flagSample = string.Join("; ", Data.Take(10).Select((line, i) => $"pos{i + 1}/WMS{line.LineSequenceNumber}/{line.MaterialCode} usedBin={line.IsLocationUsedBin} ({line.LocationUsedBin})"));
                throw new Exception($"Cannot post VRA Item Fulfillment: mixed bin-tracked flags in one save (location '{Data.FirstOrDefault()?.LocationName}'). Reload the order so every line resolves the same location, then retry. Sample: {flagSample}.");
            }

            //string url = string.Format(isUsedBin ? ItemFulfillmentUrl : ItemFulfillmentUrlNotUsedBin, "vendorReturnAuthorization", orderId);
            // Drop ?replace= per bucket when that bucket carries no inventoryDetail, otherwise NetSuite rejects the transform with INVALID_PARAMETER.

            var badTO = Data.Where(x => x.IsBad).ToList();

            if (badTO.Any(x => x.ScannedQuantity > 0))
            {
                // NetSuite rejects inventoryDetail without binNumber at bin-tracked locations ("configure the inventory detail in line N").
                var badVraMissingBin = new List<string>();
                for (var i = 0; i < badTO.Count; i++)
                {
                    var line = badTO[i];
                    if (RequiresBin(isUsedBin, line.ScannedQuantity, line.NetsuiteMaterialVendorAssignedBin, line.NetsuiteMaterialPrefferedBinId))
                    {
                        badVraMissingBin.Add($"item-list line {i + 1} (WMS line {line.LineSequenceNumber}) {line.MaterialCode} - {line.MaterialName}");
                    }
                }

                if (badVraMissingBin.Any())
                {
                    throw new Exception($"Cannot post BAD VRA Item Fulfillment: location '{Data.FirstOrDefault()?.LocationName}' is bin-tracked but no bin is resolved for: {string.Join("; ", badVraMissingBin)}. Reload the order; if the bin is still missing, ask Master Data to set a Vendor Bin Assignment or a preferred bin for these items.");
                }

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
                    var diag = $"[diag replace={urlBad.Contains("replace=")} usedBin={isUsedBin} scannedLines={badTO.Count(x => x.ScannedQuantity > 0)} totalLines={badTO.Count} build={BuildStamp}]";

                    // Decode NetSuite's "line N of the item list" into the actual DTO row so field tests identify the offender without server logs.
                    var nsLineMatch = Regex.Match(ex.Message, @"line (\d+) of the item list");
                    if (nsLineMatch.Success && int.TryParse(nsLineMatch.Groups[1].Value, out var nsLine) && nsLine >= 1 && nsLine <= badTO.Count)
                    {
                        var offender = badTO[nsLine - 1];
                        diag += $" | line{nsLine}=WMSLine {offender.LineSequenceNumber} {offender.MaterialCode} - {offender.MaterialName} vendorBin={offender.NetsuiteMaterialVendorAssignedBin} preferredBin={offender.NetsuiteMaterialPrefferedBinId} qty={offender.ScannedQuantity}";
                    }

                    throw new Exception($"Error while posting BAD VRA Item Fulfillment. {ex.Message} {diag}", ex);
                }
            }

            var goodTO = Data.Where(x => !x.IsBad).ToList();

            if (goodTO.Any())
            {
                // NetSuite rejects inventoryDetail without binNumber at bin-tracked locations ("configure the inventory detail in line N").
                var goodVraMissingBin = new List<string>();
                for (var i = 0; i < goodTO.Count; i++)
                {
                    var line = goodTO[i];
                    if (RequiresBin(isUsedBin, line.ScannedQuantity, line.NetsuiteMaterialVendorAssignedBin, line.NetsuiteMaterialPrefferedBinId))
                    {
                        goodVraMissingBin.Add($"item-list line {i + 1} (WMS line {line.LineSequenceNumber}) {line.MaterialCode} - {line.MaterialName}");
                    }
                }

                if (goodVraMissingBin.Any())
                {
                    throw new Exception($"Cannot post GOOD VRA Item Fulfillment: location '{Data.FirstOrDefault()?.LocationName}' is bin-tracked but no bin is resolved for: {string.Join("; ", goodVraMissingBin)}. Reload the order; if the bin is still missing, ask Master Data to set a Vendor Bin Assignment or a preferred bin for these items.");
                }

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
                    var diag = $"[diag replace={urlGood.Contains("replace=")} usedBin={isUsedBin} scannedLines={goodTO.Count(x => x.ScannedQuantity > 0)} totalLines={goodTO.Count} build={BuildStamp}]";

                    // Decode NetSuite's "line N of the item list" into the actual DTO row so field tests identify the offender without server logs.
                    var nsLineMatch = Regex.Match(ex.Message, @"line (\d+) of the item list");
                    if (nsLineMatch.Success && int.TryParse(nsLineMatch.Groups[1].Value, out var nsLine) && nsLine >= 1 && nsLine <= goodTO.Count)
                    {
                        var offender = goodTO[nsLine - 1];
                        diag += $" | line{nsLine}=WMSLine {offender.LineSequenceNumber} {offender.MaterialCode} - {offender.MaterialName} vendorBin={offender.NetsuiteMaterialVendorAssignedBin} preferredBin={offender.NetsuiteMaterialPrefferedBinId} qty={offender.ScannedQuantity}";
                    }

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

            // Only lines the operator touched. Untouched lines must never be sent as 0.
            // The web page does not set IsCounted, so its existing ScannedQuantity filter still works.
            var countableIC = Data.Where(x => x.IsCounted || x.ScannedQuantity > 0).ToList();

            if (countableIC.Any())
            {
                try
                {
                    var payload = PatchInventoryCountingPayloadDTO.PatchInventoryCounting(countableIC);

                    var jsonString = JsonSerializer.Serialize(payload, JsonSerializerOption);

                    await MakeRequest<object>(url, jsonString, HttpMethod.Patch);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error while posting Inventory Counting. {ex.Message}", ex);
                }
            }

            return true;
        }

        public async Task<bool> PostInventoryWorksheet(List<InventoryWorksheetLineDTO> Data, int Location, int Subsidiary)
        {
            string url = InventoryWorksheetRestletUrl;

            PostInventoryWorksheetPayload? payloadGood = null;

            try
            {
                payloadGood = PostInventoryWorksheetPayload.PostInventoryWorksheet(Data, Location, Subsidiary);

                var jsonStringGood = JsonSerializer.Serialize(payloadGood, JsonSerializerOption);

                await MakeRequest<object>(url, jsonStringGood, HttpMethod.Post);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error while posting Inventory Worksheet. {ex.Message} {BuildWorksheetDiag(payloadGood, Data)}", ex);
            }

            return true;
        }

        /// <summary>
        /// Names the worksheet line most likely to exceed NetSuite's adjustqtyby
        /// character limit, so a failed save is diagnosable from the operator's
        /// screen when server logs are unavailable. The length is measured on the
        /// serialized decimal, because decimal scale survives System.Text.Json
        /// serialization and is what actually reaches the wire.
        /// </summary>
        private static string BuildWorksheetDiag(PostInventoryWorksheetPayload? payload, List<InventoryWorksheetLineDTO> data)
        {
            try
            {
                if (payload is null || payload.Lines.Count == 0)
                {
                    var zeroQty = data.Count(x => x.GoodScannedQuantity <= 0 && x.BadScannedQuantity <= 0);
                    return $"[diag lines=0 in={data.Count} zeroQty={zeroQty} build={BuildStamp}]";
                }

                static int Len(decimal value) => JsonSerializer.Serialize(value).Length;

                var worst = payload.Lines
                    .SelectMany(line => line.InventoryDetail
                        .Select(detail => (line.ItemId, Qty: detail.Quantity, Len: Len(detail.Quantity)))
                        .Append((line.ItemId, Qty: line.NewQuantity, Len: Len(line.NewQuantity))))
                    .OrderByDescending(entry => entry.Len)
                    .FirstOrDefault();

                var maxLen = payload.Lines
                    .SelectMany(line => line.InventoryDetail
                        .Select(detail => Len(detail.Quantity))
                        .Append(Len(line.NewQuantity)))
                    .DefaultIfEmpty(0)
                    .Max();

                var qty = worst.Qty.ToString(System.Globalization.CultureInfo.InvariantCulture);

                return $"[diag lines={payload.Lines.Count} " +
                       $"scanned={data.Count(x => x.GoodScannedQuantity > 0 || x.BadScannedQuantity > 0)} " +
                       $"item={worst.ItemId} qty={qty} len={worst.Len} maxLen={maxLen} build={BuildStamp}]";
            }
            catch
            {
                return $"[diag unavailable build={BuildStamp}]";
            }
        }
        #endregion

    }
}
