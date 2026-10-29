using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.SupplierReturn;
using Integration.NS.DataTransferObjects.Others;
using Integration.NS.DataTransferObjects.SupplierReturn;
using Integration.NS.Implementations.Others;
using Integration.NS.Implementations.Transactions;
using Integration.NS.Services;
using Integration.NS.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Shared.Entities;
using Shared.Libraries.Utilities;
using Xunit;

namespace Integration.NS.Tests.Implementations.Transactions;

public class SupplierReturnPreferredVendorValidationTests
{
    private static (SupplierReturnIntegration Sut, RestCapturingNetSuiteClient Client) Create(
        int preferredVendorId = 42,
        int locationSubsidiaryId = 7,
        int? poLineVendorId = 42,
        int? poHeaderVendorId = 42,
        int? poHeaderSubsidiaryId = 7)
    {
        var client = new RestCapturingNetSuiteClient
        {
            OnQuery = (query, type) =>
            {
                if (type == typeof(SupplierReturnPreferredVendorLineNSDTO))
                {
                    return new NetSuiteResponse<SupplierReturnPreferredVendorLineNSDTO>
                    {
                        items =
                        [
                            new SupplierReturnPreferredVendorLineNSDTO
                            {
                                ItemId = 101,
                                ItemCode = "ITEM-101",
                                LineNumber = 4,
                                LocationId = 5,
                                VendorId = poLineVendorId,
                                PurchaseOrderVendorId = poHeaderVendorId,
                                PurchaseOrderSubsidiaryId = poHeaderSubsidiaryId
                            }
                        ],
                        totalResults = 1
                    };
                }

                if (type == typeof(ItemsNSDTO))
                {
                    var item = query.Contains("aggregateitemlocation", StringComparison.OrdinalIgnoreCase)
                        ? new ItemsNSDTO { Id = 101, QuantityOnHand = 5 }
                        : new ItemsNSDTO
                        {
                            Id = 101,
                            PreferredVendorId = preferredVendorId,
                            SubsidiaryId = locationSubsidiaryId
                        };
                    return new NetSuiteResponse<ItemsNSDTO> { items = [item], totalResults = 1 };
                }

                return null;
            }
        };
        var sut = new SupplierReturnIntegration(client, new HttpContextAccessor(), new SuiteQLQueryBuilderFactoryService());
        return (sut, client);
    }

    private static SupplierReturnDTO SampleReturn(int lineVendorId = 42) => new()
    {
        Vendor = new VendorDTO { Id = 42 },
        Subsidiary = new SubsidiaryDTO { Id = 7 },
        Location = new LocationDTO { Id = 5 },
        Lines =
        [
            new SupplierReturnLineDTO
            {
                ItemId = 101,
                ItemCode = "ITEM-101",
                LineNumber = 1,
                Vendor = new VendorDTO { Id = lineVendorId },
                QuantityAlloted = 1
            }
        ]
    };

    [Fact]
    public async Task Create_rejects_item_with_a_different_preferred_vendor_before_posting()
    {
        var (sut, client) = Create(preferredVendorId: 99);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateSupplierReturn(SampleReturn()));

        Assert.Contains("not under the selected preferred vendor", error.Message);
        Assert.Empty(client.Writes);
        Assert.Contains(client.Queries, query => query.Contains("itemvendor")
            && query.Contains("iv.subsidiary = (SELECT l.subsidiary FROM location l WHERE l.id = 5)")
            && query.Contains("item.id IN"));
    }

    [Fact]
    public async Task Create_rejects_line_vendor_that_differs_from_the_selected_header_vendor()
    {
        var (sut, client) = Create();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateSupplierReturn(SampleReturn(lineVendorId: 99)));

        Assert.Contains("not under the selected preferred vendor", error.Message);
        Assert.Empty(client.Writes);
    }

    [Fact]
    public async Task Direct_standard_create_rejects_invalid_items_before_posting()
    {
        var (sut, client) = Create(preferredVendorId: 99);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateNewSupplierReturn(SampleReturn()));

        Assert.Contains("not under the selected preferred vendor", error.Message);
        Assert.Empty(client.Writes);
    }

    [Fact]
    public async Task Create_rejects_when_warehouse_and_selected_subsidiary_do_not_match()
    {
        var (sut, client) = Create(locationSubsidiaryId: 8);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateSupplierReturn(SampleReturn()));

        Assert.Contains("not under the selected preferred vendor", error.Message);
        Assert.Empty(client.Writes);
    }

    [Fact]
    public async Task Create_allows_a_line_when_header_line_and_master_preferred_vendors_match()
    {
        var (sut, client) = Create();

        await sut.CreateSupplierReturn(SampleReturn());

        Assert.Single(client.Writes);
        Assert.Contains("custcol_dbti_vendor", client.LastBody);
    }

    [Fact]
    public async Task PO_transform_validates_actual_purchase_order_lines_before_posting()
    {
        var (sut, client) = Create(preferredVendorId: 99);
        var data = SampleReturn();
        data.SourcePO = 456;

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateSupplierReturnFromPurchaseOrder(data));

        Assert.Contains("not under the selected preferred vendor", error.Message);
        Assert.Empty(client.Writes);
        Assert.Contains(client.Queries, query => query.Contains("t.id = 456") && query.Contains("purchaseorder"));
    }

    [Fact]
    public async Task PO_transform_rejects_header_vendor_that_differs_from_the_actual_purchase_order()
    {
        var (sut, client) = Create(poHeaderVendorId: 99);
        var data = SampleReturn();
        data.SourcePO = 456;

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateSupplierReturnFromPurchaseOrder(data));

        Assert.Contains("does not match the source purchase order", error.Message);
        Assert.Empty(client.Writes);
    }

    [Fact]
    public async Task PO_transform_posts_when_actual_header_and_item_preferred_vendors_match()
    {
        var (sut, client) = Create();
        var data = SampleReturn();
        data.SourcePO = 456;

        await sut.CreateSupplierReturnFromPurchaseOrder(data);

        Assert.Single(client.Writes);
        Assert.Contains(client.Queries, query => query.Contains("t.id = 456") && query.Contains("purchaseorder"));
    }

    [Fact]
    public async Task Approval_rejects_item_without_a_preferred_vendor_before_patching()
    {
        var (sut, client) = Create(preferredVendorId: 0);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.SubmitSupplierReturnForApproval(SampleReturn()));

        Assert.Contains("not under the selected preferred vendor", error.Message);
        Assert.Empty(client.Writes);
    }

    [Fact]
    public async Task Item_picker_filter_resolves_to_the_preferred_vendor_column()
    {
        var client = new CapturingNetSuiteClient();
        var sut = new ItemsIntegration(client, new SuiteQLQueryBuilderFactoryService());
        var intent = new DataGridIntent
        {
            Filters = [DataGridFilterUtilities.Equal(nameof(ItemsDTO.PreferredVendorId), 42)],
            Take = 20
        };

        await sut.GetItemsByLocationDataGridAsync(intent, location: 5, subsidiary: 7);

        Assert.Contains(client.Queries, query => query.Contains("pv.vendor = 42"));
    }
}
