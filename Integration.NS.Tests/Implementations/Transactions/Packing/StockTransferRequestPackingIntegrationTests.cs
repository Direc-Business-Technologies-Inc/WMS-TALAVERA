using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.Packing.STR;
using Database.Libraries.Repositories;
using Integration.NS.DataTransferObjects.Packing.STR;
using Integration.NS.Exceptions;
using Integration.NS.Implementations.Transactions.Packing;
using Integration.NS.Services;
using Integration.NS.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Entities;
using Shared.Libraries.Utilities;
using System.Data;
using Xunit;

namespace Integration.NS.Tests.Implementations.Transactions.Packing;

/// <summary>
/// Offset-out-of-bounds recovery for the Packing STR list grid (same defect
/// family as the receiving lists): NetSuite rejects any SuiteQL offset past
/// the end of its result set, and persisted Radzen pager positions request
/// such offsets once the result set shrinks. The integration must clamp to
/// the last legal page from NetSuite's "between 0 and N" hint, retry once,
/// and surface the exception when no better page exists.
/// </summary>
public class StockTransferRequestPackingIntegrationTests
{
    private static (StockTransferRequestPackingIntegration Sut, CapturingNetSuiteClient Client) CreateSut()
    {
        var client = new CapturingNetSuiteClient();
        var sut = new StockTransferRequestPackingIntegration(
            client,
            new HttpContextAccessor(),
            new StubSqlQueryManager(),
            new SuiteQLQueryBuilderFactoryService(),
            NullLogger<StockTransferRequestPackingIntegration>.Instance);
        return (sut, client);
    }

    private static DataGridIntent Intent(int skip, int take) => new()
    {
        Skip = skip,
        Take = take
    };

    private static StrPackingDataGridNSDTO Row(int id) => new()
    {
        Id = id,
        ReferenceNumber = $"ISTR-{id}"
    };

    // ---------------------------------------------------------------------
    // An out-of-bounds offset is clamped to the last legal page from
    // NetSuite's "between 0 and N" hint and retried once; the retried page's
    // rows and total are returned.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Out_of_bounds_offset_is_clamped_and_retried_once()
    {
        var (sut, client) = CreateSut();

        // Skip=40 with Take=20 is a legal multiple, so ExecuteWithPaging issues
        // exactly one call — which NetSuite rejects because the result set ends
        // at offset 20.
        client.NextException = new NetSuiteValidationException(
            "NetSuite 404 Not Found: [INVALID_PARAMETER] The specified query parameter 'offset' is out of bounds. Provide value between 0 and 20",
            "The specified query parameter 'offset' is out of bounds. Provide value between 0 and 20",
            NetSuiteErrorCode.OffsetOutOfBounds);
        client.NextItems = [Row(1), Row(2)];

        var (data, count) = await sut.GetPackingStockTransferRequestList(Intent(skip: 40, take: 20), subsidiaryId: 1);

        Assert.Equal([(20, 40), (20, 20)], client.Calls);
        Assert.Equal(2, count);
        Assert.Equal(2, data.Count());
    }

    // ---------------------------------------------------------------------
    // When the clamp would return the very offset that already failed, retrying
    // cannot improve the outcome — the exception must surface instead of
    // looping or reporting a silent empty page.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Clamp_that_matches_failed_offset_rethrows()
    {
        var (sut, client) = CreateSut();

        // Bound 0 with Take=20 floors to skip 0... use bound=20: floor(20/20)*20=20.
        // Skip=20 already failed, so retrying at 20 is pointless.
        client.NextException = new NetSuiteValidationException(
            "NetSuite 404 Not Found: [INVALID_PARAMETER] The specified query parameter 'offset' is out of bounds. Provide value between 0 and 20",
            "The specified query parameter 'offset' is out of bounds. Provide value between 0 and 20",
            NetSuiteErrorCode.OffsetOutOfBounds);

        var exception = await Assert.ThrowsAsync<NetSuiteValidationException>(
            () => sut.GetPackingStockTransferRequestList(Intent(skip: 20, take: 20), subsidiaryId: 1));

        Assert.Equal(NetSuiteErrorCode.OffsetOutOfBounds, exception.ErrorCode);
        Assert.Single(client.Calls);
    }

    // ---------------------------------------------------------------------
    // A missing "between 0 and N" hint falls back to the first page.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Clamp_without_bound_hint_falls_back_to_first_page()
    {
        var (sut, client) = CreateSut();

        client.NextException = new NetSuiteValidationException(
            "NetSuite 404 Not Found: [INVALID_PARAMETER] The specified query parameter 'offset' is out of bounds.",
            "The specified query parameter 'offset' is out of bounds.",
            NetSuiteErrorCode.OffsetOutOfBounds);
        client.NextItems = [Row(7)];

        var (data, count) = await sut.GetPackingStockTransferRequestList(Intent(skip: 60, take: 20), subsidiaryId: 1);

        Assert.Equal([(20, 60), (20, 0)], client.Calls);
        Assert.Equal(1, count);
        Assert.Equal("ISTR-7", data.Single().ReferenceNumber);
    }

    // ---------------------------------------------------------------------
    // A well-formed request must not change behavior: one call, rows mapped.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task In_bounds_request_passes_through_untouched()
    {
        var (sut, client) = CreateSut();

        client.NextItems = [Row(1)];

        var (data, count) = await sut.GetPackingStockTransferRequestList(Intent(skip: 0, take: 20), subsidiaryId: 1);

        Assert.Equal([(20, 0)], client.Calls);
        Assert.Equal(1, count);
        Assert.Equal("ISTR-1", data.Single().ReferenceNumber);
    }

    private sealed class StubSqlQueryManager : ISqlQueryManager
    {
        public Dictionary<string, string> GetSqlScriptWithMetadata(string scriptFileName, out string sqlContent, out bool isScriptFound, string scriptFilePath)
        {
            throw new NotSupportedException();
        }

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
