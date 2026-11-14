using System.Text.Json;
using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Transactions.Receiving;
using Application.UseCases.Commands.Transaction.Receiving;
using Integration.NS.Implementations.Transactions;
using Integration.NS.Services;
using Integration.NS.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static Integration.NS.Implementations.Transactions.ReceivingIntegration;

namespace Integration.NS.Tests.Implementations.Transactions;

public class ReceivingPostingTests
{
    private static (ReceivingIntegration Sut, RestCapturingNetSuiteClient Client) Create()
    {
        var client = new RestCapturingNetSuiteClient();
        client.QueryItems.Add(new ItemFulfillmentLineRemainingDTO
        {
            LineNumber = 10,
            ItemCode = "ITEM-1",
            ItemDescription = "Test item",
            QuantityPlanned = 10,
            QuantityFulfilled = 10,
            QuantityReceived = 0
        });
        client.QueryItems.Add(new OperationalTransferLink
        {
            SourceId = 123, ShippingLine = 9, OperationalLine = 0,
            ReceivingLine = 10, ItemId = 49312, UnitId = 1
        });
        client.QueryItems.Add(new OperationalSourceDocument { TranId = "TO123" });
        client.QueryItems.Add(new OperationalDocumentIdentity { Id = 456 });
        // Retain each test's fresh-quantity setup, but run it through the actual
        // REST operational reader and TO metadata mapping rather than bypassing it.
        client.OnRead = url =>
        {
            var remaining = client.QueryItems.OfType<ItemFulfillmentLineRemainingDTO>().Single();
            var receipt = url.Contains("itemReceipt");
            return JsonSerializer.SerializeToElement(new
            {
                id = receipt ? 789 : 456, createdFrom = new { id = "123" },
                item = new { items = new[] { new
                {
                    line = 0, orderLine = receipt ? 10 : 9, item = new { id = "49312" },
                    units = new { id = "1" }, quantity = receipt ? remaining.QuantityReceived : remaining.QuantityFulfilled
                } } }
            });
        };
        client.OnQuery = (query, type) =>
        {
            var remaining = client.QueryItems.OfType<ItemFulfillmentLineRemainingDTO>().SingleOrDefault();
            if (remaining is null) return null;
            if (type == typeof(TransferOrderLineRemaining))
                return new Application.DataTransferObjects.Others.NS.NetSuiteResponse<TransferOrderLineRemaining>
                { items = [new() { LineId = 10, QuantityPlanned = 10 }], totalResults = 1 };
            if (type == typeof(ItemReceiptLineDTO))
                return new Application.DataTransferObjects.Others.NS.NetSuiteResponse<ItemReceiptLineDTO>
                {
                    items = [new() { LineNumber = 10, ItemId = 49312, ItemCode = "ITEM-1",
                        UoMRate = 1, QuantityPlanned = remaining.QuantityPlanned }], totalResults = 1
                };
            if (type == typeof(OperationalReceiptLink))
                return new Application.DataTransferObjects.Others.NS.NetSuiteResponse<OperationalReceiptLink>
                {
                    items = remaining.QuantityReceived > 0 ? [new() { FulfillmentId = 456, ReceiptId = 789 }] : [],
                    totalResults = remaining.QuantityReceived > 0 ? 1 : 0
                };
            if (type == typeof(OperationalReceiptLineLink))
                return new Application.DataTransferObjects.Others.NS.NetSuiteResponse<OperationalReceiptLineLink>
                { items = [new() { SourceId = 123, ReceivingLine = 10, OperationalLine = 0 }], totalResults = 1 };
            if (type == typeof(OperationalTransferLink))
                return new Application.DataTransferObjects.Others.NS.NetSuiteResponse<OperationalTransferLink>
                { items = [new() { SourceId = 123, ShippingLine = 9, OperationalLine = 0,
                    ReceivingLine = 10, ItemId = 49312, UnitId = 1 }], totalResults = 1 };
            if (type == typeof(OperationalSourceDocument))
                return new Application.DataTransferObjects.Others.NS.NetSuiteResponse<OperationalSourceDocument>
                { items = [new() { TranId = "TO123" }], totalResults = 1 };
            return null;
        };
        return (new ReceivingIntegration(client, new HttpContextAccessor(),
            new SuiteQLQueryBuilderFactoryService(), NullLogger<ReceivingIntegration>.Instance), client);
    }

    private static InventoryDetailDTO Detail(int status, decimal quantity) => new()
    {
        Status = new() { Id = status }, QuantityAlloted = quantity
    };

    private static ItemReceiptDTO Receipt(ItemReceiptDTO.SourceTypes type = ItemReceiptDTO.SourceTypes.PurchaseOrder) => new()
    {
        SourceType = type,
        SourceInternalId = 123,
        ItemFulfillmentId = type == ItemReceiptDTO.SourceTypes.PurchaseOrder ? null : 456,
        Lines = [new()
        {
            LineNumber = 10, ItemCode = "ITEM-1", QuantityPlanned = 10,
            QuantityAlloted = 5, InventoryDetails = [Detail(1, 5)]
        }]
    };

    [Theory]
    [InlineData("empty")]
    [InlineData("zero")]
    [InlineData("unchecked")]
    [InlineData("missing-details")]
    [InlineData("missing-status")]
    [InlineData("unsupported-status")]
    [InlineData("negative")]
    [InlineData("negative-detail")]
    [InlineData("mismatch")]
    [InlineData("over-remaining")]
    [InlineData("line-id")]
    [InlineData("source-id")]
    [InlineData("duplicate")]
    public async Task Invalid_receipts_fail_before_any_NetSuite_call(string scenario)
    {
        var (sut, client) = Create();
        var dto = Receipt();
        var line = dto.Lines[0];
        switch (scenario)
        {
            case "empty": dto.Lines.Clear(); break;
            case "zero": line.QuantityAlloted = 0; line.InventoryDetails.Clear(); break;
            case "unchecked": line.IsReceived = false; break;
            case "missing-details": line.InventoryDetails.Clear(); break;
            case "missing-status": line.InventoryDetails[0].Status = null; break;
            case "unsupported-status": line.InventoryDetails[0].Status!.Id = 99; break;
            case "negative": line.QuantityAlloted = -1; break;
            case "negative-detail": line.InventoryDetails = [Detail(1, 6), Detail(3, -1)]; break;
            case "mismatch": line.InventoryDetails = [Detail(1, 4)]; break;
            case "over-remaining": line.QuantityReceived = 6; break;
            case "line-id": line.LineNumber = 0; break;
            case "source-id": dto.SourceInternalId = 0; break;
            case "duplicate": dto.Lines.Add(line); break;
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.PostItemReceipt(dto));
        Assert.Empty(client.Writes);
        Assert.Equal(0, client.QueryCount);
    }

    [Theory]
    [InlineData(ItemReceiptDTO.SourceTypes.TransferOrder, null)]
    [InlineData(ItemReceiptDTO.SourceTypes.TransferOrder, -1)]
    [InlineData(ItemReceiptDTO.SourceTypes.Returns, 0)]
    public async Task Transfer_receipts_require_a_positive_fulfillment_id(ItemReceiptDTO.SourceTypes type, int? fulfillmentId)
    {
        var (sut, client) = Create();
        var dto = Receipt(type);
        dto.ItemFulfillmentId = fulfillmentId;
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.PostItemReceipt(dto));
        Assert.Contains("Item Fulfillment", ex.Message);
        Assert.Empty(client.Writes);
        Assert.Equal(0, client.QueryCount);
    }

    [Fact]
    public async Task Positive_non_bin_receipt_posts_with_status_and_accepts_empty_success_body()
    {
        var (sut, client) = Create();
        Assert.True(await sut.PostItemReceipt(Receipt()));
        var write = Assert.Single(client.Writes);
        Assert.Equal(HttpMethod.Post, write.Method);
        Assert.DoesNotContain("replace=", write.Url);
        using var body = JsonDocument.Parse(write.Body!);
        var item = body.RootElement.GetProperty("item").GetProperty("items")[0];
        Assert.True(item.GetProperty("itemreceive").GetBoolean());
        Assert.Equal(5, item.GetProperty("quantity").GetDecimal());
    }

    [Fact]
    public async Task PO_retains_unchecked_fillers_but_never_posts_their_status_bucket()
    {
        var (sut, client) = Create();
        var dto = Receipt();
        dto.Lines.Add(new()
        {
            LineNumber = 20, QuantityPlanned = 10, IsReceived = false,
            QuantityAlloted = 2, InventoryDetails = [Detail(3, 2)]
        });
        Assert.True(await sut.PostItemReceipt(dto));
        var write = Assert.Single(client.Writes);
        using var body = JsonDocument.Parse(write.Body!);
        var items = body.RootElement.GetProperty("item").GetProperty("items");
        Assert.Equal(2, items.GetArrayLength());
        Assert.False(items[1].GetProperty("itemreceive").GetBoolean());
    }

    [Fact]
    public async Task PO_later_bucket_omits_lines_fully_received_by_the_earlier_bucket()
    {
        var (sut, client) = Create();
        var dto = Receipt();
        dto.Lines[0].QuantityPlanned = 5;
        dto.Lines[0].InventoryDetails = [Detail(3, 5)];
        dto.Lines.Add(new()
        {
            LineNumber = 20, QuantityPlanned = 10, QuantityAlloted = 3,
            InventoryDetails = [Detail(1, 3)]
        });
        Assert.True(await sut.PostItemReceipt(dto));
        Assert.Equal(2, client.Writes.Count);
        using var body = JsonDocument.Parse(client.Writes[1].Body!);
        var items = body.RootElement.GetProperty("item").GetProperty("items");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal(20, items[0].GetProperty("orderLine").GetInt32());
    }

    [Fact]
    public async Task Bin_tracked_PO_rejects_missing_bin_before_any_write()
    {
        var (sut, client) = Create();
        var dto = Receipt();
        dto.Lines[0].IsLocationBinUsed = true;
        dto.Lines[0].ItemUseBins = "T";
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.PostItemReceipt(dto));
        Assert.Contains("valid bin", ex.Message);
        Assert.Empty(client.Writes);
    }

    [Fact]
    public async Task Bin_tracked_PO_uses_inventory_assignment_replace_url()
    {
        var (sut, client) = Create();
        var dto = Receipt();
        dto.Lines[0].IsLocationBinUsed = true;
        dto.Lines[0].InventoryDetails[0].Bin = new() { Id = 7 };
        Assert.True(await sut.PostItemReceipt(dto));
        Assert.Contains("?replace=item.inventoryDetail.inventoryAssignment", client.LastUrl);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(6)]
    public async Task Bin_tracked_PO_replace_covers_unreceived_lines_with_empty_assignments(int status)
    {
        var (sut, client) = Create();
        var dto = Receipt();
        dto.Lines[0].IsLocationBinUsed = true;
        dto.Lines[0].ItemUseBins = "T";
        dto.Lines[0].InventoryDetails = [Detail(status, 5)];
        dto.Lines[0].InventoryDetails[0].Bin = new() { Id = 7 };
        dto.Lines.Add(new()
        {
            LineNumber = 20, QuantityPlanned = 10, IsReceived = false,
            QuantityAlloted = 2, InventoryDetails = [Detail(status, 2)]
        });

        Assert.True(await sut.PostItemReceipt(dto));
        var write = Assert.Single(client.Writes);
        Assert.Contains("?replace=item.inventoryDetail.inventoryAssignment", write.Url);
        using var body = JsonDocument.Parse(write.Body!);
        var items = body.RootElement.GetProperty("item").GetProperty("items");
        Assert.Equal(2, items.GetArrayLength());
        Assert.True(items[0].GetProperty("itemreceive").GetBoolean());
        var assigned = items[0].GetProperty("inventoryDetail").GetProperty("inventoryAssignment")
            .GetProperty("items");
        Assert.Equal(1, assigned.GetArrayLength());
        Assert.Equal(status, assigned[0].GetProperty("inventoryStatus").GetInt32());
        Assert.Equal(7, assigned[0].GetProperty("binNumber").GetInt32());
        Assert.False(items[1].GetProperty("itemreceive").GetBoolean());
        Assert.False(items[1].TryGetProperty("quantity", out _));
        Assert.Equal(0, items[1].GetProperty("inventoryDetail").GetProperty("inventoryAssignment")
            .GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task Non_bin_PO_keeps_plain_transform_and_detail_free_fillers()
    {
        var (sut, client) = Create();
        var dto = Receipt();
        dto.Lines.Add(new() { LineNumber = 20, QuantityPlanned = 10, IsReceived = false });

        Assert.True(await sut.PostItemReceipt(dto));
        var write = Assert.Single(client.Writes);
        Assert.DoesNotContain("replace=", write.Url);
        using var body = JsonDocument.Parse(write.Body!);
        var items = body.RootElement.GetProperty("item").GetProperty("items");
        Assert.Equal(5, items[0].GetProperty("inventoryDetail").GetProperty("inventoryAssignment")
            .GetProperty("items")[0].GetProperty("quantity").GetDecimal());
        Assert.False(items[1].GetProperty("itemreceive").GetBoolean());
        Assert.False(items[1].TryGetProperty("inventoryDetail", out _));
    }

    [Fact]
    public async Task Bin_tracked_PO_mixed_status_buckets_keep_fillers_empty_and_received_assignments()
    {
        var (sut, client) = Create();
        var dto = Receipt();
        dto.Lines[0].IsLocationBinUsed = true;
        dto.Lines[0].ItemUseBins = "T";
        dto.Lines[0].InventoryDetails = [Detail(3, 2), Detail(1, 3)];
        foreach (var detail in dto.Lines[0].InventoryDetails)
            detail.Bin = new() { Id = 7 };
        dto.Lines.Add(new() { LineNumber = 20, QuantityPlanned = 10, IsReceived = false });

        Assert.True(await sut.PostItemReceipt(dto));
        Assert.Equal(2, client.Writes.Count);
        foreach (var (write, status) in client.Writes.Zip(new[] { 3, 1 }))
        {
            Assert.Contains("?replace=item.inventoryDetail.inventoryAssignment", write.Url);
            using var body = JsonDocument.Parse(write.Body!);
            var items = body.RootElement.GetProperty("item").GetProperty("items");
            Assert.Equal(2, items.GetArrayLength());
            Assert.True(items[0].GetProperty("itemreceive").GetBoolean());
            var assigned = items[0].GetProperty("inventoryDetail").GetProperty("inventoryAssignment")
                .GetProperty("items");
            Assert.Equal(1, assigned.GetArrayLength());
            Assert.Equal(status, assigned[0].GetProperty("inventoryStatus").GetInt32());
            Assert.False(items[1].GetProperty("itemreceive").GetBoolean());
            Assert.Equal(0, items[1].GetProperty("inventoryDetail").GetProperty("inventoryAssignment")
                .GetProperty("items").GetArrayLength());
        }
    }

    private static ItemReceiptDTO MixedReceipt()
    {
        var dto = Receipt(ItemReceiptDTO.SourceTypes.TransferOrder);
        dto.Lines[0].InventoryDetails = [Detail(3, 1), Detail(1, 3), Detail(6, 1)];
        return dto;
    }

    [Fact]
    public async Task Transfer_receipt_allows_the_remaining_quantity_for_the_selected_fulfillment()
    {
        var (sut, client) = Create();
        client.QueryItems.Clear();
        client.QueryItems.Add(new ItemFulfillmentLineRemainingDTO
        {
            LineNumber = 10,
            ItemCode = "ITEM-1",
            ItemDescription = "Test item",
            QuantityPlanned = 9,
            QuantityFulfilled = 9,
            QuantityReceived = 6
        });
        var dto = Receipt(ItemReceiptDTO.SourceTypes.TransferOrder);
        dto.Lines[0].QuantityPlanned = 9;
        dto.Lines[0].QuantityReceived = 6;
        dto.Lines[0].QuantityAlloted = 3;
        dto.Lines[0].InventoryDetails = [Detail(1, 3)];

        Assert.True(await sut.PostItemReceipt(dto));
        Assert.Single(client.Writes);
    }

    [Fact]
    public async Task Transfer_receipt_rejects_quantity_above_the_selected_fulfillment_remainder()
    {
        var (sut, client) = Create();
        client.QueryItems.Clear();
        client.QueryItems.Add(new ItemFulfillmentLineRemainingDTO
        {
            LineNumber = 10,
            ItemCode = "ITEM-1",
            ItemDescription = "Test item",
            QuantityPlanned = 9,
            QuantityFulfilled = 9,
            QuantityReceived = 6
        });
        var dto = Receipt(ItemReceiptDTO.SourceTypes.TransferOrder);
        dto.Lines[0].QuantityPlanned = 9;
        // Simulate a stale browser that still believes no receipt has been
        // applied; the server's fresh IF-scoped check must reject the overage.
        dto.Lines[0].QuantityReceived = 0;
        dto.Lines[0].QuantityAlloted = 4;
        dto.Lines[0].InventoryDetails = [Detail(1, 4)];

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.PostItemReceipt(dto));

        Assert.Contains("remaining quantity 3", error.Message);
        Assert.Empty(client.Writes);
    }

    [Fact]
    public async Task Transfer_receipt_is_capped_to_the_quantity_shipped_on_this_fulfillment()
    {
        var (sut, client) = Create();
        client.QueryItems.Clear();
        client.QueryItems.Add(new ItemFulfillmentLineRemainingDTO
        {
            LineNumber = 10,
            ItemCode = "ITEM-1",
            ItemDescription = "Test item",
            QuantityPlanned = 9,
            QuantityFulfilled = 3,
            QuantityReceived = 0
        });
        var dto = Receipt(ItemReceiptDTO.SourceTypes.TransferOrder);
        dto.Lines[0].QuantityPlanned = 9;
        dto.Lines[0].QuantityAlloted = 4;
        dto.Lines[0].InventoryDetails = [Detail(1, 4)];

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.PostItemReceipt(dto));

        Assert.Contains("remaining quantity 3", error.Message);
        Assert.Empty(client.Writes);
    }

    [Fact]
    public async Task Buckets_wait_for_the_previous_response_before_starting()
    {
        var (sut, client) = Create();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        client.OnWrite = count => count == 1 ? release.Task : Task.CompletedTask;
        var posting = sut.PostItemReceipt(MixedReceipt());
        try
        {
            Assert.Single(client.Writes);
            Assert.False(posting.IsCompleted);
        }
        finally { release.SetResult(); }
        Assert.True(await posting);
        Assert.Equal(new[] { 3, 1, 5 }, client.Writes.Select(write =>
        {
            using var body = JsonDocument.Parse(write.Body!);
            return body.RootElement.GetProperty("transferCategory").GetInt32();
        }));
    }

    [Theory]
    [InlineData(1, "No earlier status bucket")]
    [InlineData(2, "Partial receipt: already posted BAD/CONFISCATED")]
    public async Task Failed_bucket_stops_later_posts_and_preserves_cause(int failAt, string expected)
    {
        var (sut, client) = Create();
        var cause = new Exception("NetSuite rejected this receipt");
        client.OnWrite = count => count == failAt ? Task.FromException(cause) : Task.CompletedTask;
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.PostItemReceipt(MixedReceipt()));
        Assert.Contains(expected, ex.Message);
        Assert.Contains("Check NetSuite Item Receipts before retrying", ex.Message);
        Assert.Same(cause, ex.InnerException);
        Assert.Equal(failAt, client.Writes.Count);
    }

    [Fact]
    public async Task Invalid_later_bucket_prevents_all_writes()
    {
        var (sut, client) = Create();
        var dto = MixedReceipt();
        dto.Lines[0].InventoryDetails[1].Status = null;
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.PostItemReceipt(dto));
        Assert.Empty(client.Writes);
    }

    [Fact]
    public async Task Transfer_payload_excludes_unchecked_lines_and_zero_allocations()
    {
        var (sut, client) = Create();
        var dto = Receipt(ItemReceiptDTO.SourceTypes.TransferOrder);
        dto.Lines[0].InventoryDetails.Add(Detail(3, 0));
        dto.Lines.Add(new() { LineNumber = 20, IsReceived = false, QuantityAlloted = 2,
            InventoryDetails = [Detail(3, 2)] });
        Assert.True(await sut.PostItemReceipt(dto));
        var write = Assert.Single(client.Writes);
        using var body = JsonDocument.Parse(write.Body!);
        var lines = body.RootElement.GetProperty("lines");
        Assert.Equal(1, lines.GetArrayLength());
        Assert.Equal(456, body.RootElement.GetProperty("fulfillmentId").GetInt32());
        Assert.Equal(1, lines[0].GetProperty("inventoryDetail").GetArrayLength());
    }

    [Fact]
    public async Task Command_never_converts_an_exception_into_success()
    {
        var (sut, client) = Create();
        client.OnWrite = _ => Task.FromException(new Exception("Empty response from NetSuite API"));
        var handler = new CreateItemReceiptCmdHandler(sut);
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(new(Receipt()), CancellationToken.None));
    }
}
