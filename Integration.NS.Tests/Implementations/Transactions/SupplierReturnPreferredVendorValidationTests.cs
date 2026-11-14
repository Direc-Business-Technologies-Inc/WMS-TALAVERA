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
        int? preferredVendorId = 42,
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

    private static SupplierReturnDTO SampleReturn(int? lineVendorId = 42) => new()
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
                Vendor = lineVendorId.HasValue ? new VendorDTO { Id = lineVendorId.Value } : null,
                QuantityAlloted = 1
            }
        ]
    };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Create_rejects_zero_quantity_lines_before_contacting_netsuite(bool fromPurchaseOrder)
    {
        var (sut, client) = Create();
        var data = SampleReturn();
        if (fromPurchaseOrder) data.SourcePO = 123;
        data.Lines = [data.Lines.First(), new SupplierReturnLineDTO
        {
            ItemId = 101, ItemCode = "ITEM-101", LineNumber = 2, QuantityAlloted = 0
        }];

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateSupplierReturn(data));

        Assert.Contains("line(s): 2", error.Message);
        Assert.Empty(client.Writes);
        Assert.Empty(client.Queries);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Create_rejects_empty_line_list_before_contacting_netsuite(bool fromPurchaseOrder)
    {
        var (sut, client) = Create();
        var data = SampleReturn();
        if (fromPurchaseOrder) data.SourcePO = 123;
        data.Lines = [];

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateSupplierReturn(data));

        Assert.Contains("Add at least one RTS line", error.Message);
        Assert.Empty(client.Writes);
        Assert.Empty(client.Queries);
    }

    [Fact]
    public async Task Create_rejects_item_with_a_different_preferred_vendor_before_posting()
    {
        var (sut, client) = Create(preferredVendorId: 99);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateSupplierReturn(SampleReturn()));

        Assert.Contains("item preferred vendor 99 does not match selected header vendor 42", error.Message);
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

        Assert.Contains("RTS line vendor 99 does not match selected header vendor 42", error.Message);
        Assert.Empty(client.Writes);
    }

    [Fact]
    public async Task Direct_standard_create_rejects_invalid_items_before_posting()
    {
        var (sut, client) = Create(preferredVendorId: 99);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateNewSupplierReturn(SampleReturn()));

        Assert.Contains("item preferred vendor 99 does not match selected header vendor 42", error.Message);
        Assert.Empty(client.Writes);
    }

    [Fact]
    public async Task Create_rejects_when_warehouse_and_selected_subsidiary_do_not_match()
    {
        var (sut, client) = Create(locationSubsidiaryId: 8);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateSupplierReturn(SampleReturn()));

        Assert.Contains("location 5 resolves to subsidiary 8, not selected subsidiary 7", error.Message);
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

        Assert.Contains("item preferred vendor 99 does not match selected header vendor 42", error.Message);
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
        var (sut, client) = Create(preferredVendorId: null);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.SubmitSupplierReturnForApproval(SampleReturn()));

        Assert.Contains("item has no preferred vendor for location 5's subsidiary", error.Message);
        Assert.DoesNotContain("RTS line vendor is missing", error.Message);
        Assert.Empty(client.Writes);
    }

    [Fact]
    public async Task Approval_allows_missing_transaction_line_vendor_when_item_master_vendor_matches()
    {
        var (sut, client) = Create();

        await sut.SubmitSupplierReturnForApproval(SampleReturn(lineVendorId: null));

        Assert.Single(client.Writes);
        Assert.Contains("custbody_dbti_submitted_for_approval", client.LastBody);
    }

    [Fact]
    public async Task Approve_sets_the_custom_approval_list_value_for_submitted_accessible_RTS()
    {
        var client = new RestCapturingNetSuiteClient();
        client.QueryItems.Add(new SupplierReturnApprovalStateNSDTO
        {
            Id = 123,
            SubsidiaryId = 7,
            SubmittedForApprovals = "T",
            ApprovalStatusId = 3
        });
        var sut = new SupplierReturnIntegration(client, new HttpContextAccessor(), new SuiteQLQueryBuilderFactoryService());

        await sut.ApproveSupplierReturnAsync(123, [7]);

        Assert.Single(client.Writes);
        Assert.Equal(HttpMethod.Patch, client.LastMethod);
        Assert.Contains("vendorReturnAuthorization/123", client.LastUrl);
        Assert.Contains("custbody_dbti_custom_approval_status", client.LastBody);
        Assert.Contains("4", client.LastBody);
        Assert.Contains(client.Queries, query => query.Contains("custbody_dbti_submitted_for_approval")
            && query.Contains("t.subsidiary IN (7)"));
    }

    [Fact]
    public async Task Approve_rejects_unsubmitted_RTS_without_patching()
    {
        var client = new RestCapturingNetSuiteClient();
        client.QueryItems.Add(new SupplierReturnApprovalStateNSDTO
        {
            Id = 123,
            SubsidiaryId = 7,
            SubmittedForApprovals = "F",
            ApprovalStatusId = 3
        });
        var sut = new SupplierReturnIntegration(client, new HttpContextAccessor(), new SuiteQLQueryBuilderFactoryService());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ApproveSupplierReturnAsync(123, [7]));

        Assert.Contains("Submit this RTS for approval", error.Message);
        Assert.Empty(client.Writes);
    }

    [Fact]
    public async Task Update_backfills_missing_transaction_line_vendor_from_header_after_validation()
    {
        var (sut, client) = Create();
        var data = SampleReturn(lineVendorId: null);
        data.Id = 123;

        await sut.UpdateSupplierReturn(data);

        Assert.Single(client.Writes);
        Assert.Contains("custcol_dbti_vendor", client.LastBody);
        Assert.Contains("42", client.LastBody);
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
