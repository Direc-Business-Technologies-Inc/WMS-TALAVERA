using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using Integration.NS.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Integration.NS.Tests.Services;

public class NetSuiteApiClientServiceTokenTests
{
    private static T GetStaticField<T>(string name)
    {
        var field = typeof(NetSuiteApiClientService)
            .GetField(name, BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"Field '{name}' not found.");

        return (T)field.GetValue(null)!;
    }

    private static NetSuiteApiClientService CreateService()
    {
        return new NetSuiteApiClientService(
            new HttpContextAccessor(),
            null!,
            NullLogger<NetSuiteApiClientService>.Instance);
    }

    // Task 6 / Req 3.3 — the shared HttpClient has an explicit timeout.
    [Fact]
    public void HttpClient_has_explicit_timeout_of_100_seconds_by_default()
    {
        Assert.Null(Environment.GetEnvironmentVariable("NETSUITE_HTTP_TIMEOUT_SECONDS"));

        var client = GetStaticField<HttpClient>("_httpClient");

        Assert.Equal(TimeSpan.FromSeconds(100), client.Timeout);
    }

    [Fact]
    public void Http_timeout_seconds_is_env_configurable()
    {
        var original = Environment.GetEnvironmentVariable("NETSUITE_HTTP_TIMEOUT_SECONDS");
        try
        {
            Environment.SetEnvironmentVariable("NETSUITE_HTTP_TIMEOUT_SECONDS", "45");
            Assert.Equal(45, NetSuiteApiClientService.GetHttpTimeoutSeconds());

            Environment.SetEnvironmentVariable("NETSUITE_HTTP_TIMEOUT_SECONDS", "not-a-number");
            Assert.Equal(100, NetSuiteApiClientService.GetHttpTimeoutSeconds());

            Environment.SetEnvironmentVariable("NETSUITE_HTTP_TIMEOUT_SECONDS", null);
            Assert.Equal(100, NetSuiteApiClientService.GetHttpTimeoutSeconds());
        }
        finally
        {
            Environment.SetEnvironmentVariable("NETSUITE_HTTP_TIMEOUT_SECONDS", original);
        }
    }

    // Task 6 / Req 3.2 — the RSA key file is read once and the imported key reused.
    [Fact]
    public void Rsa_key_is_loaded_once_and_cached()
    {
        var originalPath = Environment.GetEnvironmentVariable("PRIVATEKEY_PATH");
        var keyFile = Path.Combine(Path.GetTempPath(), $"ns-test-key-{Guid.NewGuid():N}.pem");
        try
        {
            using (var generator = RSA.Create(3072))
            {
                var pem =
                    "-----BEGIN PRIVATE KEY-----\n" +
                    Convert.ToBase64String(
                        generator.ExportPkcs8PrivateKey(),
                        Base64FormattingOptions.InsertLineBreaks) +
                    "\n-----END PRIVATE KEY-----\n";
                File.WriteAllText(keyFile, pem);
            }

            Environment.SetEnvironmentVariable("PRIVATEKEY_PATH", keyFile);

            var lazy = GetStaticField<Lazy<RSA>>("_rsaKey");

            var first = lazy.Value;

            // If the loader re-read the file, deleting it would make the second
            // access throw. The cached Lazy must return the same instance.
            File.Delete(keyFile);
            var second = lazy.Value;

            Assert.Same(first, second);
            Assert.True(lazy.IsValueCreated);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PRIVATEKEY_PATH", originalPath);
            if (File.Exists(keyFile))
                File.Delete(keyFile);
        }
    }

    // Task 6 / Req 3.1 — GetAccessToken must not set the expiry; only a
    // successful refresh through GetValidAccessToken owns that.
    [Fact]
    public async Task GetAccessToken_does_not_set_expiry()
    {
        var originalPath = Environment.GetEnvironmentVariable("PRIVATEKEY_PATH");
        var keyFile = Path.Combine(Path.GetTempPath(), $"ns-test-key-{Guid.NewGuid():N}.pem");
        try
        {
            using (var generator = RSA.Create(3072))
            {
                var pem =
                    "-----BEGIN PRIVATE KEY-----\n" +
                    Convert.ToBase64String(
                        generator.ExportPkcs8PrivateKey(),
                        Base64FormattingOptions.InsertLineBreaks) +
                    "\n-----END PRIVATE KEY-----\n";
                File.WriteAllText(keyFile, pem);
            }

            Environment.SetEnvironmentVariable("PRIVATEKEY_PATH", keyFile);

            var service = CreateService();
            var before = GetStaticField<DateTime>("_tokenExpiryTime");

            var token = await service.GetAccessToken((_, _) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"access_token\":\"test-token\",\"expires_in\":3600}")
                }));

            var after = GetStaticField<DateTime>("_tokenExpiryTime");

            Assert.Equal("test-token", token);
            Assert.Equal(before, after);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PRIVATEKEY_PATH", originalPath);
            if (File.Exists(keyFile))
                File.Delete(keyFile);
        }
    }

    // Task 5 / Req 4.2 — the parsed user-visible message format is unchanged.
    [Fact]
    public void BuildNetSuiteErrorMessage_preserves_format_with_detail()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            ReasonPhrase = "Bad Request"
        };

        var message = NetSuiteApiClientService.BuildNetSuiteErrorMessage(
            response,
            "Insufficient stock",
            "{\"o:errorDetails\":[]}");

        Assert.Equal("NetSuite 400 Bad Request: Insufficient stock", message);
    }

    [Fact]
    public void BuildNetSuiteErrorMessage_falls_back_to_body_excerpt()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            ReasonPhrase = "Internal Server Error"
        };

        var message = NetSuiteApiClientService.BuildNetSuiteErrorMessage(
            response,
            null,
            "raw body");

        Assert.Equal("NetSuite 500 Internal Server Error: raw body", message);
    }

    [Fact]
    public void BuildNetSuiteErrorMessage_handles_empty_body()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            ReasonPhrase = "Service Unavailable"
        };

        var message = NetSuiteApiClientService.BuildNetSuiteErrorMessage(
            response,
            null,
            string.Empty);

        Assert.Equal("NetSuite 503 Service Unavailable: empty response body", message);
    }
}
