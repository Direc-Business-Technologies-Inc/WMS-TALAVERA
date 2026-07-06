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
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using static Application.DataTransferObjects.Transactions.Commons.NS.ReturnsEnum;

namespace Integration.NS.Services
{
    public class NetSuiteApiClientService(HttpContextAccessor httpContextAccessor, ISqlQueryManager sqlQuery) : INetSuiteApiClientService, INotifyPropertyChanged
    {

        string NsDateTimeFormat = "YYYY-MM-DD HH24:MI:SS";
        string DateTimeISOFormat = "YYYY-MM-DD\"T\"HH24:MI:SS";

        private static readonly string AccountId = Environment.GetEnvironmentVariable("ACCOUNT_ID") ?? string.Empty;

        private static readonly string ItemFulfillmentUrl = $"https://{AccountId}.suitetalk.api.netsuite.com/services/rest/record/v1/{{0}}/{{1}}/!transform/itemFulfillment";

        private static readonly string PatchItemFulfillmentUrl = $"https://{AccountId}.suitetalk.api.netsuite.com/services/rest/record/v1/itemFulfillment/{{0}}";

        private static readonly string PatchInventoryCountUrl = $"https://{AccountId}.suitetalk.api.netsuite.com/services/rest/record/v1/inventorycount/{{0}}";

        private static readonly string ItemReceiptUrl = $"https://{AccountId}.suitetalk.api.netsuite.com/services/rest/record/v1/{{0}}/{{1}}/!transform/itemReceipt";

        private static readonly string UpdateRecordUrl = $"https://{AccountId}.suitetalk.api.netsuite.com/services/rest/record/v1/{{0}}/{{1}}";

        private static readonly string ClientCredentialsCertificateId = Environment.GetEnvironmentVariable("NETSUITE_CERTIFICATE_ID") ?? string.Empty;

        private static readonly string ItemReceiptRestletUrl = $"https://{AccountId}.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=1853&deploy=1";

        private static readonly string TripTicketRestletUrl = $"https://{AccountId}.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=1862&deploy=1";

        private static readonly string InventoryWorksheetRestletUrl = $"https://{AccountId}.restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=1878&deploy=1";

        private static readonly string ApiConsumerKey = Environment.GetEnvironmentVariable("NETSUITE_CONSUMER_KEY") ?? string.Empty;

        private static readonly string PrivateKeyPem = Environment.GetEnvironmentVariable("NETSUITE_PRIVATE_KEY") ?? string.Empty;

        private static readonly string RestApiRoot = $"https://{AccountId}.suitetalk.api.netsuite.com/services/rest";

        private static string Oauth2ApiRoot = $"{RestApiRoot}/auth/oauth2/v1";
        private static string RecordApiRoot = $"{RestApiRoot}/record/v1";

        private static string TokenEndPointUrl = $"{Oauth2ApiRoot}/token";

        private static readonly HttpClient _httpClient = new HttpClient();

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

        public async Task<string> GetAccessToken()
        {

            var url = Oauth2ApiRoot + "/token";
            string clientAssertion = GetJwtToken();

            _tokenExpiryTime = DateTime.UtcNow.AddMinutes(30);

            var requestParams = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"),
                new KeyValuePair<string, string>("client_assertion", clientAssertion)
            };

            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, url);

            httpRequest.Content = new FormUrlEncodedContent(requestParams);

            var httpResponse = await _httpClient.SendAsync(httpRequest);
            string res = await httpResponse.Content.ReadAsStringAsync();
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
                { "scope", "rest_webservices" }
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

        public async Task<T> MakeRequest<T>(string url, string? reqBody, HttpMethod method)
        {
            if (_accessToken == null || DateTime.Now >= _tokenExpiryTime)
                _accessToken = await GetAccessToken();

            using var httpRequest = new HttpRequestMessage(method, url);

            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            // Add other custom headers
            httpRequest.Headers.Add("Prefer", "transient");

            if (!string.IsNullOrEmpty(reqBody))
            {
                //_logger.LogDebug("SuiteQLQuery Request: {@Request}", reqBody);
                httpRequest.Content = new StringContent(reqBody, Encoding.UTF8, "application/json");
            }

            var httpResponse = await _httpClient.SendAsync(httpRequest);

            var responseJson = await httpResponse.Content.ReadAsStringAsync();

            if (httpResponse.IsSuccessStatusCode)
            {
                //_logger.LogDebug("SuiteQLQuery Result: {@Result}", responseJson);
                if (string.IsNullOrWhiteSpace(responseJson))
                {
                    return default;
                }

                var response = JsonSerializer.Deserialize<T>(responseJson, JsonSerializerRequestOption);
                if (response == null) throw new Exception("Bad response from NetSuite API");
                return response;
            }

            var errorDetail = GetNetSuiteErrorDetail(responseJson);

            //throw new Exception($"Request failed with status code: {httpResponse.StatusCode}");
            throw new Exception(errorDetail);
        }

        public async Task<T> MakeRequestOAuth1<T>(string url, string? reqBody)
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

            // Signature base string
            string signatureBaseString =
                $"POST&{Uri.EscapeDataString(baseUrl)}&{Uri.EscapeDataString(normalizedParameters)}";

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

            using var request = new HttpRequestMessage(HttpMethod.Post, url);

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

            var httpResponse = await _httpClient.SendAsync(request);

            var responseJson = await httpResponse.Content.ReadAsStringAsync();
            if (httpResponse.IsSuccessStatusCode)
            {
                if (string.IsNullOrEmpty(responseJson))
                    return default(T);

                return System.Text.Json.JsonSerializer.Deserialize<T>(responseJson);
            }

            var errorDetail = GetNetSuiteErrorDetail(responseJson);

            //throw new Exception($"Request failed with status code: {httpResponse.StatusCode}");
            throw new Exception(errorDetail);
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
                if (root.TryGetProperty("error", out var errorElement) &&
                    errorElement.ValueKind == JsonValueKind.String)
                {
                    var error = errorElement.GetString();

                    if (!string.IsNullOrWhiteSpace(error))
                        return error;
                }

                //// Optional fallback: title
                //if (root.TryGetProperty("title", out var titleElement) &&
                //    titleElement.ValueKind == JsonValueKind.String)
                //{
                //    var title = titleElement.GetString();

                //    if (!string.IsNullOrWhiteSpace(title))
                //        return title;
                //}

                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<IEnumerable<T>?> NetsuiteQuery<T>(
            string queryName,
            Dictionary<string, string>? parameters = null,
            int limit = 0,
            int offset = 0)
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

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    query = query.Replace(
                        $"@{parameter.Key}",
                        $"'{parameter.Value.Replace("'", "''")}'");

                    query = query.Replace(
                        $"{{{parameter.Key}}}",
                        parameter.Value);
                }
            }

            var jsonBody = System.Text.Json.JsonSerializer.Serialize(new
            {
                q = FormatQuery(query)
            });

            var result = await MakeRequest<NetSuiteResponse<T>>(url, jsonBody, HttpMethod.Post);

            return result.items;
        }

        public async Task<NetSuiteResponse<T>> ExecuteSuiteQLQuery<T>(string query, int? limit = null, int? offset = null)
        {
            var url = SuiteQLRoot;
            if (limit.HasValue) url += $"?limit={limit.Value}" + (offset.HasValue ? $"&offset={offset.Value}" : "");
            else if (offset.HasValue) url += $"?offset={offset.Value}";

            query = FormatQuery(query);

            var jsonBody = JsonSerializer.Serialize(new { q = query });

            return await MakeRequest<NetSuiteResponse<T>>(url, jsonBody, HttpMethod.Post);
        }

        #region Receiving
        public async Task<bool> SavePOItemReceipt(List<PostPurchaseOrderDTO> Data)
        {

            var orderId = Data.Select(x => x.NetsuiteOrderInternalId).FirstOrDefault();
            string url = string.Format(ItemReceiptUrl, "purchaseOrder", orderId);

            var badPO = Data.Where(x => x.IsBad).ToList();

            if (badPO.Any(x => x.ScannedQuantity > 0))
            {
                try
                {
                    var payloadBad = PurchaseOrderIRPayloadDTO.CreateForItemReceipt(badPO, 2);

                    var jsonStringBad = JsonSerializer.Serialize(payloadBad, JsonSerializerOption);

                    await MakeRequest<object>(url, jsonStringBad, HttpMethod.Post);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error while posting BAD PO Item Receipt. {ex.Message}", ex);
                }
            }

            var goodPO = Data.Where(x => !x.IsBad).ToList();

            if (goodPO.Any())
            {
                try
                {
                    var payloadGood = PurchaseOrderIRPayloadDTO.CreateForItemReceipt(goodPO, 1);

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

        public async Task<bool> SaveTOItemReceipt(List<PostTransferOrderDTO> Data)
        {
            var orderId = Data.Select(x => x.NetsuiteOrderInternalId).FirstOrDefault();
            string url = ItemReceiptRestletUrl;

            var badTO = Data.Where(x => x.IsBad).ToList();

            if (badTO.Any(x => x.ScannedQuantity > 0))
            {
                try
                {
                    var payloadBad = TransferOrderIRRestletPayloadDTO.CreateForItemReceiptRestlet(badTO, orderId, 3);

                    var jsonStringBad = JsonSerializer.Serialize(payloadBad, JsonSerializerOption);

                    await MakeRequestOAuth1<object>(url, jsonStringBad);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error while posting BAD TO Item Receipt. {ex.Message}", ex);
                }
            }

            var goodTO = Data.Where(x => !x.IsBad).ToList();

            if (goodTO.Any())
            {
                try
                {
                    var payloadGood = TransferOrderIRRestletPayloadDTO.CreateForItemReceiptRestlet(goodTO, orderId, 1);

                    var jsonStringGood = JsonSerializer.Serialize(payloadGood, JsonSerializerOption);

                    await MakeRequestOAuth1<object>(url, jsonStringGood);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error while posting GOOD TO Item Receipt. {ex.Message}", ex);
                }
            }

            return true;
        }

        public async Task<bool> SaveReturnsItemReceipt(List<PostReturnsDTO> Data)
        {
            var orderId = Data.Select(x => x.NetsuiteOrderInternalId).FirstOrDefault();
            string url = string.Format(ItemReceiptUrl, "transferOrder", orderId);

            try
            {
                var transferCategory = (TransferCategory)Data.Select(x => x.TransferCategory).FirstOrDefault();

                var receivingCategory = transferCategory == TransferCategory.GoodItems ? 1 : 2;

                ReturnsIRPayloadDTO payloadGood = ReturnsIRPayloadDTO.CreateForItemReceipt(Data, receivingCategory);

                var jsonStringGood = JsonSerializer.Serialize(payloadGood, JsonSerializerOption);

                await MakeRequest<object>(url, jsonStringGood, HttpMethod.Post);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error while posting Returns Item Receipt. {ex.Message}", ex);
            }

            return true;
        }
        #endregion

        #region Itemfulfillment
        public async Task<bool> SaveTOItemFulfillment(List<PostTransferOrderDTO> Data)
        {
            var orderId = Data.Select(x => x.NetsuiteOrderInternalId).FirstOrDefault();
            string url = string.Format(ItemFulfillmentUrl, "transferOrder", orderId);

            var badTO = Data.Where(x => x.IsBad).ToList();

            if (badTO.Any(x => x.ScannedQuantity > 0))
            {
                try
                {
                    var payloadBad = TransferOrderIFPayloadDTO.CreateForItemFulfillment(badTO, "B");

                    var jsonStringBad = JsonSerializer.Serialize(payloadBad, JsonSerializerOption);

                    await MakeRequest<object>(url, jsonStringBad, HttpMethod.Post);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error while posting BAD TO Item Fulfillment. {ex.Message}", ex);
                }
            }

            var goodTO = Data.Where(x => !x.IsBad).ToList();

            if (goodTO.Any())
            {
                try
                {
                    var payloadGood = TransferOrderIFPayloadDTO.CreateForItemFulfillment(goodTO, "B");

                    var jsonStringGood = JsonSerializer.Serialize(payloadGood, JsonSerializerOption);

                    await MakeRequest<object>(url, jsonStringGood, HttpMethod.Post);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error while posting GOOD TO Item Fulfillment. {ex.Message}", ex);
                }
            }
            return true;
        }

        public async Task<bool> SaveReturnsItemFulfillment(List<PostReturnsDTO> Data)
        {
            var orderId = Data.Select(x => x.NetsuiteOrderInternalId).FirstOrDefault();
            string url = string.Format(ItemFulfillmentUrl, "transferOrder", orderId);

            try
            {
                ReturnsIFPayloadDTO payloadGood = ReturnsIFPayloadDTO.CreateForItemFulfillment(Data, "B");

                var jsonStringGood = JsonSerializer.Serialize(payloadGood, JsonSerializerOption);

                await MakeRequest<object>(url, jsonStringGood, HttpMethod.Post);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error while posting Returns Item Fulfillment. {ex.Message}", ex);
            }

            return true;
        }

        public async Task<bool> SaveVRAItemFulfillment(List<PostVendorReturnAuthorizationDTO> Data)
        {
            var orderId = Data.Select(x => x.NetsuiteOrderInternalId).FirstOrDefault();
            string url = string.Format(ItemFulfillmentUrl, "vendorReturnAuthorization", orderId);

            var badTO = Data.Where(x => x.IsBad).ToList();

            if (badTO.Any(x => x.ScannedQuantity > 0))
            {
                try
                {
                    var payloadBad = VendorReturnAuthorizationIFPayloadDTO.CreateForItemFulfillment(badTO, "B");

                    var jsonStringBad = JsonSerializer.Serialize(payloadBad, JsonSerializerOption);

                    await MakeRequest<object>(url, jsonStringBad, HttpMethod.Post);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error while posting BAD VRA Item Fulfillment. {ex.Message}", ex);
                }
            }

            var goodTO = Data.Where(x => !x.IsBad).ToList();

            if (goodTO.Any())
            {
                try
                {
                    var payloadGood = VendorReturnAuthorizationIFPayloadDTO.CreateForItemFulfillment(goodTO, "B");

                    var jsonStringGood = JsonSerializer.Serialize(payloadGood, JsonSerializerOption);

                    await MakeRequest<object>(url, jsonStringGood, HttpMethod.Post);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error while posting GOOD VRA Item Fulfillment. {ex.Message}", ex);
                }
            }
            return true;
        }
        #endregion

        #region TripTicket
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

                    await MakeRequestOAuth1<object>(url, jsonStringGood);
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

            //var badPO = Data.Where(x => x.IsBad).ToList();

            //if (badPO.Any(x => x.ScannedQuantity > 0))
            //{
            //    var payloadBad = PurchaseOrderIRPayloadDTO.CreateForItemReceipt(badPO, 2);

            //    var jsonStringBad = JsonSerializer.Serialize(payloadBad, JsonSerializerOption);

            //    await MakeRequest<object>(url, jsonStringBad);
            //}

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

                await MakeRequestOAuth1<object>(url, jsonStringGood);
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
