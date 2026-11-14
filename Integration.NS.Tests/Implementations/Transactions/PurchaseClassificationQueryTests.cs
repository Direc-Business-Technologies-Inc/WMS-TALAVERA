using Integration.NS.DataTransferObjects.Others;
using Integration.NS.DataTransferObjects.Receiving;
using Integration.NS.Implementations.Others;
using Integration.NS.Implementations.Transactions;
using Integration.NS.Services;
using Integration.NS.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Entities;
using Shared.Libraries.Utilities;
using Xunit;

namespace Integration.NS.Tests.Implementations.Transactions;

public class PurchaseClassificationQueryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Item_grids_map_classification_filters_to_real_columns_before_paging(bool scoped)
    {
        var client = new CapturingNetSuiteClient
        {
            NextResult = new ItemsNSDTO { Id = 13643, PurchaseCategoryId = 1, PurchaseSubcategoryId = 1 }
        };
        var sut = new ItemsIntegration(client, new SuiteQLQueryBuilderFactoryService());
        var intent = new DataGridIntent
        {
            Take = 20,
            Skip = 20,
            Filters = [
                DataGridFilterUtilities.Equal(nameof(ItemsNSDTO.PurchaseCategoryId), 1),
                DataGridFilterUtilities.Equal(nameof(ItemsNSDTO.PurchaseSubcategoryId), 1),
                DataGridFilterUtilities.Contains(nameof(ItemsNSDTO.Name), "MAGGI")
            ]
        };
        if (scoped)
        {
            intent.Filters.Add(DataGridFilterUtilities.GreaterThan(nameof(ItemsNSDTO.QuantityAvailable), 0));
            intent.Filters.Add(DataGridFilterUtilities.Equal(nameof(ItemsNSDTO.PreferredVendorId), 42));
        }

        var result = scoped
            ? await sut.GetItemsByLocationDataGridAsync(intent, 5, 1)
            : await sut.GetItemsDataGridAsync(intent);

        Assert.Contains("psc.id = i.custitem_dbti_item_group", client.LastQuery);
        var query = Assert.IsType<string>(client.LastQuery);
        var where = query[query.LastIndexOf(" WHERE ", StringComparison.Ordinal)..];
        Assert.Contains("psc.custrecord_dbti_psc_purchase_category = 1", where);
        Assert.Contains("i.custitem_dbti_item_group = 1", where);
        Assert.Contains("MAGGI", where);
        if (scoped)
        {
            Assert.Contains("loc.id = 5", where);
            Assert.Contains("ail.quantityavailable > 0", where);
            Assert.Contains("pv.vendor = 42", where);
        }
        Assert.Equal(20, client.LastLimit);
        Assert.Equal(20, client.LastOffset);
        var item = Assert.Single(result.Data);
        Assert.Equal(1, item.PurchaseCategoryId);
        Assert.Equal(1, item.PurchaseSubcategoryId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(5)]
    public async Task Barcode_lookup_returns_item_classification_without_restricting_other_scanner_consumers(int? location)
    {
        var client = new CapturingNetSuiteClient
        {
            NextResult = new BarcodeNSDTO { ItemId = 13643, PurchaseCategoryId = 1, PurchaseSubcategoryId = 1 }
        };
        var sut = new ReceivingIntegration(client, new HttpContextAccessor(),
            new SuiteQLQueryBuilderFactoryService(), NullLogger<ReceivingIntegration>.Instance);

        var barcode = await sut.GetBarcodeData("48025522", location);

        Assert.Contains("psc.id = item.custitem_dbti_item_group", client.LastQuery);
        Assert.Equal(1, barcode!.Item!.PurchaseCategoryId);
        Assert.Equal(1, barcode.Item.PurchaseSubcategoryId);
        var query = Assert.IsType<string>(client.LastQuery);
        var where = query[query.LastIndexOf(" WHERE ", StringComparison.Ordinal)..];
        Assert.DoesNotContain("psc.", where);
    }
}
