using System.Text.Json;
using Application.DataTransferObjects.Others.NS;
using Integration.NS.DataTransferObjects.StockTransferRequest;
using Integration.NS.Implementations.Transactions;
using Integration.NS.Services;
using Integration.NS.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Integration.NS.Tests.Implementations.Transactions;

public class FulfillmentExemptionRuleReadTests
{
    [Fact]
    public async Task Reads_all_pages_so_a_later_conflicting_rule_cannot_be_missed()
    {
        var client = new RestCapturingNetSuiteClient();
        var rows = Enumerable.Range(1, 1001).Select(id => new FulfillmentExemptionRuleNSDTO
        {
            RuleId = id,
            TransferCategoryId = Value(1),
            FromLocationId = Value(10),
            ToLocationId = Value(20),
            AutomatedPackingValue = Value(2),
            AutomatedShippingValue = Value(2),
            LabelTemplateId = Value(501),
            IsInactive = "F"
        }).ToList();
        client.OnQuery = (_, type) => type == typeof(FulfillmentExemptionRuleNSDTO)
            ? Page(rows, client.QueryCount == 1 ? 0 : 1000, client.QueryCount == 1 ? 1000 : 1)
            : null;

        var sut = Create(client);
        var result = await sut.GetActiveFulfillmentExemptionRules();

        Assert.Equal(1001, result.Count);
        Assert.Equal("1001", result[^1].RuleId);
        Assert.Equal("2", result[^1].AutomatedPackingValue);
        Assert.Equal(2, client.QueryCount);
        Assert.Contains("customrecord_fulfillment_excemption", client.Queries[0]);
        Assert.Contains("custrecord_fe_label_template", client.Queries[0]);
    }

    [Fact]
    public async Task Missing_suiteql_item_array_fails_closed()
    {
        var client = new CapturingNetSuiteClient { NextNullItems = true };
        var sut = Create(client);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetActiveFulfillmentExemptionRules());
    }

    [Fact]
    public async Task Empty_page_with_reported_records_fails_instead_of_skipping_rules()
    {
        var client = new RestCapturingNetSuiteClient
        {
            OnQuery = (_, type) => type == typeof(FulfillmentExemptionRuleNSDTO)
                ? new NetSuiteResponse<FulfillmentExemptionRuleNSDTO>
                {
                    items = [],
                    totalResults = 1
                }
                : null
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => Create(client).GetActiveFulfillmentExemptionRules());
    }

    private static JsonElement Value(int value) => JsonDocument.Parse(value.ToString()).RootElement.Clone();

    private static NetSuiteResponse<FulfillmentExemptionRuleNSDTO> Page(
        List<FulfillmentExemptionRuleNSDTO> rows, int offset, int count) => new()
    {
        items = rows.Skip(offset).Take(count).ToList(),
        totalResults = rows.Count,
        hasMore = offset + count < rows.Count
    };

    private static StockTransferRequestIntegration Create(Application.UseCases.Repositories.Integration.Others.INetSuiteApiClientService client) =>
        new(client, new HttpContextAccessor(), new SuiteQLQueryBuilderFactoryService());
}
