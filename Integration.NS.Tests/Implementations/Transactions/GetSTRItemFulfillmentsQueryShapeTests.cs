using Application.DataTransferObjects.Transactions.Receiving;
using Integration.NS.Implementations.Transactions;
using Integration.NS.Services;
using Integration.NS.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Entities;
using Xunit;

namespace Integration.NS.Tests.Implementations.Transactions;

public sealed class GetSTRItemFulfillmentsQueryShapeTests
{
    [Fact]
    public async Task Picker_scopes_quantity_queries_and_pages_only_pending_fulfillments()
    {
        var client = new CapturingNetSuiteClient();
        client.QueryItems = (query, type) =>
        {
            if (type == typeof(ReceivingIntegration.ItemFulfillmentQuantityDTO))
            {
                return query.Contains("receiptline.transaction IN (100,200)", StringComparison.Ordinal)
                    ?
                    [
                        new ReceivingIntegration.ItemFulfillmentQuantityDTO { Id = 100, Quantity = 3 },
                        new ReceivingIntegration.ItemFulfillmentQuantityDTO { Id = 200, Quantity = 5 }
                    ]
                    :
                    [
                        new ReceivingIntegration.ItemFulfillmentQuantityDTO { Id = 10, Quantity = 5 },
                        new ReceivingIntegration.ItemFulfillmentQuantityDTO { Id = 20, Quantity = 5 }
                    ];
            }

            if (type == typeof(ReceivingIntegration.ItemFulfillmentReceiptLinkDTO))
            {
                return
                [
                    new ReceivingIntegration.ItemFulfillmentReceiptLinkDTO { ItemFulfillmentId = 10, ItemReceiptId = 100 },
                    new ReceivingIntegration.ItemFulfillmentReceiptLinkDTO { ItemFulfillmentId = 10, ItemReceiptId = 100 },
                    new ReceivingIntegration.ItemFulfillmentReceiptLinkDTO { ItemFulfillmentId = 20, ItemReceiptId = 200 }
                ];
            }

            if (type == typeof(ItemFulfillmentDTO))
            {
                if (query.Contains("t.id IN (10)", StringComparison.Ordinal))
                    return [new ItemFulfillmentDTO { Id = 10, ReferenceNumber = "IF-10" }];

                return
                [
                    new ItemFulfillmentDTO { Id = 10, ReferenceNumber = "IF-10" },
                    new ItemFulfillmentDTO { Id = 20, ReferenceNumber = "IF-20" }
                ];
            }

            return [];
        };
        var sut = new ReceivingIntegration(
            client,
            new HttpContextAccessor(),
            new SuiteQLQueryBuilderFactoryService(),
            NullLogger<ReceivingIntegration>.Instance);

        var (items, total) = await sut.GetSTRItemFulfillments(123, new DataGridIntent());

        Assert.Equal(5, client.CallCount);
        Assert.Single(items);
        Assert.Equal(10, items.Single().Id);
        Assert.Equal(1, total);

        Assert.Contains("ml.createdfrom = 123", client.Queries[0]);
        Assert.DoesNotContain("SUM(", client.Queries[0]);
        Assert.Contains("tl.transaction IN (10,20)", client.Queries[1]);
        Assert.Contains("FROM nexttransactionlinelink iflink", client.Queries[2]);
        Assert.DoesNotContain("FROM nexttransactionlink ", client.Queries[2]);
        Assert.Contains("iflink.previousdoc IN (10,20)", client.Queries[2]);
        Assert.DoesNotContain(" JOIN transactionline ", client.Queries[2]);
        Assert.DoesNotContain(" FROM transactionline ", client.Queries[2]);
        Assert.Contains("receiptline.transaction IN (100,200)", client.Queries[3]);
        Assert.DoesNotContain("nexttransactionlink", client.Queries[3]);
        Assert.Contains("t.id IN (10)", client.Queries[4]);
        Assert.All(client.Queries, query => Assert.DoesNotContain("custbody_dbti_fully_received", query));
    }
}
