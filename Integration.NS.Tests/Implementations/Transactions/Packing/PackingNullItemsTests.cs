using Database.Libraries.Repositories;
using Integration.NS.DataTransferObjects.Packing;
using Integration.NS.Implementations.Transactions.Packing;
using Integration.NS.Services;
using Integration.NS.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Libraries.Utilities;
using System.Data;
using Xunit;

namespace Integration.NS.Tests.Implementations.Transactions.Packing;

/// <summary>
/// Null <c>items</c> defensiveness for the packing header and trip-ticket lookups.
/// </summary>
/// <remarks>
/// <c>MakeRequest</c> returns <c>default(T)</c> when NetSuite answers 2xx with an empty body,
/// and otherwise deserializes the body. A 2xx body that omits the <c>items</c> array (an error
/// envelope, a partial response) deserializes into a non-null
/// <c>NetSuiteResponse&lt;T&gt;</c> whose <c>items</c> is null. Consumers that called
/// <c>response.items.FirstOrDefault()</c> without a guard then threw
/// <c>ArgumentNullException: Value cannot be null. (Parameter 'source')</c>.
/// <para>
/// Field failure 2026-10-16: packing save for STR-THM00100000675 surfaced exactly that message
/// on the mobile. Both the API (<c>ApiResult.ServerError($"{ex.Message}")</c>) and the client
/// (<c>Toast.Error(result.ErrorMessage)</c>) pass only the message, so the operator saw an
/// unactionable LINQ error with no indication of which lookup failed or whether anything
/// had been written to NetSuite.
/// </para>
/// <para>
/// The required behaviour is to return null — "could not resolve" — which
/// <c>PostTransferOrderIFCmd</c> / <c>PostReturnsIFCmd</c> /
/// <c>PostVendorReturnAuthorizationIFCmd</c> already treat as fail-closed, with the message
/// "Could not verify trip-ticket exemption ... Nothing was saved."
/// </para>
/// </remarks>
public class PackingNullItemsTests
{
    private static StockTransferRequestPackingIntegration CreateStr(
        out CapturingNetSuiteClient client)
    {
        client = new CapturingNetSuiteClient();
        return new StockTransferRequestPackingIntegration(
            client,
            new HttpContextAccessor(),
            new StubSqlQueryManager(),
            new SuiteQLQueryBuilderFactoryService(),
            NullLogger<StockTransferRequestPackingIntegration>.Instance);
    }

    private static ReturnPackingIntegration CreateReturns(out CapturingNetSuiteClient client)
    {
        client = new CapturingNetSuiteClient();
        return new ReturnPackingIntegration(
            client,
            new HttpContextAccessor(),
            new StubSqlQueryManager(),
            new SuiteQLQueryBuilderFactoryService());
    }

    private static VendorReturnAuthorizationPackingIntegration CreateVra(
        out CapturingNetSuiteClient client)
    {
        client = new CapturingNetSuiteClient();
        return new VendorReturnAuthorizationPackingIntegration(
            client,
            new StubSqlQueryManager(),
            new HttpContextAccessor(),
            new SuiteQLQueryBuilderFactoryService());
    }

    // The regression: this threw ArgumentNullException('source') before the guard was added.
    [Fact]
    public async Task Str_trip_ticket_exemption_returns_null_when_items_is_absent()
    {
        var sut = CreateStr(out var client);
        client.NextNullItems = true;

        var result = await sut.GetTripTicketExemption("STR-THM00100000675");

        Assert.Null(result);
    }

    [Fact]
    public async Task Returns_trip_ticket_exemption_returns_null_when_items_is_absent()
    {
        var sut = CreateReturns(out var client);
        client.NextNullItems = true;

        var result = await sut.GetTripTicketExemption("STR-THM00100000684");

        Assert.Null(result);
    }

    [Fact]
    public async Task Vra_trip_ticket_exemption_returns_null_when_items_is_absent()
    {
        var sut = CreateVra(out var client);
        client.NextNullItems = true;

        var result = await sut.GetTripTicketExemption("VRA-0001");

        Assert.Null(result);
    }

    // The header lookups share the same unguarded pattern and sit on the packing load path.
    [Fact]
    public async Task Str_header_lookup_returns_null_when_items_is_absent()
    {
        var sut = CreateStr(out var client);
        client.NextNullItems = true;

        var result = await sut.GetPackingStockTransferRequest("185626");

        Assert.Null(result);
    }

    [Fact]
    public async Task Returns_header_lookup_returns_null_when_items_is_absent()
    {
        var sut = CreateReturns(out var client);
        client.NextNullItems = true;

        var result = await sut.GetPackingReturn("185626");

        Assert.Null(result);
    }

    [Fact]
    public async Task Vra_header_lookup_returns_null_when_items_is_absent()
    {
        var sut = CreateVra(out var client);
        client.NextNullItems = true;

        var result = await sut.GetPackingVendorReturnAuthorization("185626");

        Assert.Null(result);
    }

    // A present row must still resolve normally - the guard must not swallow real answers.
    [Fact]
    public async Task Trip_ticket_exemption_still_resolves_when_a_row_is_present()
    {
        var sut = CreateStr(out var client);
        client.NextItems.Add(new TripTicketExemptionNSDTO { ExemptionId = 42 });

        var result = await sut.GetTripTicketExemption("STR-THM00100000675");

        Assert.True(result);
    }

    [Fact]
    public async Task Exemption_id_zero_resolves_to_false_not_null()
    {
        var sut = CreateStr(out var client);
        client.NextItems.Add(new TripTicketExemptionNSDTO { ExemptionId = 0 });

        var result = await sut.GetTripTicketExemption("STR-THM00100000675");

        Assert.False(result);
    }

    private sealed class StubSqlQueryManager : ISqlQueryManager
    {
        public Dictionary<string, string> GetSqlScriptWithMetadata(string scriptFileName, out string sqlContent, out bool isScriptFound, string scriptFilePath)
            => throw new NotSupportedException();

        public Dictionary<string, string> SetSqlScriptWithMetadata(string scriptFileName, string scriptContent, Dictionary<string, string> metadata, string scriptFilePath)
            => throw new NotSupportedException();

        public Task<int> ExecuteAsync<T>(string fileName, CommandType commandType, string connectionString)
            => throw new NotSupportedException();

        public Task<int> ExecuteAsync<T, U>(string fileName, U parameters, CommandType commandType, string connectionString)
            => throw new NotSupportedException();

        public Task<List<T>> QueryAsync<T>(string fileName, CommandType commandType, string connectionString)
            => throw new NotSupportedException();

        public Task<List<T>> QueryAsync<T, U>(string fileName, U parameters, CommandType commandType, string connectionString)
            => throw new NotSupportedException();

        public Task<List<T>> RawQueryAsync<T>(string query, CommandType commandType, string connectionString)
            => throw new NotSupportedException();

        public Task<T?> RawQueryOneAsync<T>(string query, CommandType commandType, string connectionString)
            => throw new NotSupportedException();

        public Task<List<T>> RawQueryAsync<T, U>(string query, U parameters, CommandType commandType, string connectionString)
            => throw new NotSupportedException();

        public Task<T?> QueryOneAsync<T>(string fileName, CommandType commandType, string connectionString)
            => throw new NotSupportedException();

        public Task<T?> QueryOneAsync<T, U>(string fileName, U parameters, CommandType commandType, string connectionString)
            => throw new NotSupportedException();

        public Task<List<Dictionary<string, object>>> QueryAsync(string fileName, CommandType commandType, string connectionString)
            => throw new NotSupportedException();
    }
}