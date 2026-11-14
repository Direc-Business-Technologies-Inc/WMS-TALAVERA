using System.Reflection;
using Microsoft.AspNetCore.Components.Forms;
using NSubstitute;
using Shared.Entities;
using Web.BlazorServer.Components.Pages.Transaction.StockTransferRequest.Components;
using Web.BlazorServer.Components.Pages.Transaction.SupplierReturn.Components;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.Transaction.Receiving;
using Web.BlazorServer.ViewModels.Transaction.StockTransferRequest;
using Web.BlazorServer.ViewModels.Transaction.SupplierReturn;
using Xunit;

namespace Web.BlazorServer.Tests;

public class PurchaseClassificationItemTests
{
    static PurchaseCategoryVM Category => new() { Id = 1 };
    static PurchaseSubcategoryVM Subcategory => new() { Id = 1, PurchaseCategoryId = 1 };
    static ItemsVM Item(int id = 13643) => new()
    {
        Id = id, ItemNumber = "00009886", PurchaseCategoryId = 1, PurchaseSubcategoryId = 1,
        PreferredVendorId = 42, QuantityAvailable = 10, QuantityOnHand = 10
    };

    static bool Verify(object form, ItemsVM item)
    {
        var method = form.GetType().GetMethod("IsValidBarcode", BindingFlags.Instance | BindingFlags.NonPublic)!;
        object?[] args = [new BarcodeVM { Item = item, UoM = new ItemUnitVM { ConversionRate = 1 } }, null];
        return (bool)method.Invoke(form, args)!;
    }

    static object CreateForm(bool supplierReturn)
    {
        if (!supplierReturn)
            return new STRForm
            {
                Model = new StockTransferRequestInfoVM
                {
                    PurchaseCategory = Category, PurchaseSubcategory = Subcategory, SourceLocation = new() { Id = 5 }
                }
            };
        var model = new SupplierReturnVM
        {
            PurchaseCategory = Category, PurchaseSubcategory = Subcategory, Location = new() { Id = 5 },
            Vendor = new() { Id = 42 }, Subsidiary = new() { Id = 1 }
        };
        return new SupplierReturnForm { Model = model, EditContext = new EditContext(model) };
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Both_scanners_accept_new_matching_items_and_reject_wrong_or_missing_classification(bool supplierReturn)
    {
        var form = CreateForm(supplierReturn);
        var item = Item();
        Assert.True(Verify(form, item));
        item.PurchaseSubcategoryId = 2;
        Assert.False(Verify(form, item));
        item.PurchaseSubcategoryId = 1;
        item.PurchaseCategoryId = 3;
        Assert.False(Verify(form, item));
        item.PurchaseCategoryId = null;
        Assert.False(Verify(form, item));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Both_scanners_require_both_header_selections(bool supplierReturn)
    {
        var form = CreateForm(supplierReturn);
        if (form is STRForm str) str.Model.PurchaseSubcategory = null;
        if (form is SupplierReturnForm rts) rts.Model.PurchaseSubcategory = null;
        Assert.False(Verify(form, Item()));
    }

    [Fact]
    public void Classification_check_preserves_RTS_vendor_and_STR_availability_restrictions()
    {
        var wrongVendor = Item();
        wrongVendor.PreferredVendorId = 99;
        Assert.False(Verify(CreateForm(true), wrongVendor));
        var unavailable = Item();
        unavailable.QuantityAvailable = 0;
        Assert.False(Verify(CreateForm(false), unavailable));
    }

    [Fact]
    public async Task Submission_revalidation_rejects_missing_or_reclassified_items()
    {
        var handler = Substitute.For<IItemsHandler>();
        var item = Item();
        handler.GetItemsDataGridAsync(Arg.Any<DataGridIntent>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(((IEnumerable<ItemsVM>)new[] { item }, 1)));

        Assert.True(await PurchaseClassificationItemRules.ValidateItemsAsync(handler, [item.Id], Category, Subcategory));
        Assert.False(await PurchaseClassificationItemRules.ValidateItemsAsync(handler, [item.Id, 99], Category, Subcategory));
        item.PurchaseSubcategoryId = 2;
        Assert.False(await PurchaseClassificationItemRules.ValidateItemsAsync(handler, [item.Id], Category, Subcategory));
    }

    [Fact]
    public async Task Submission_revalidation_batches_large_documents_and_deduplicates_item_ids()
    {
        var handler = Substitute.For<IItemsHandler>();
        var batches = new List<int[]>();
        handler.GetItemsDataGridAsync(Arg.Any<DataGridIntent>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var intent = call.ArgAt<DataGridIntent>(0);
                var ids = ((IEnumerable<int>)intent.Filters.Single(f => f.Property == nameof(ItemsVM.Id)).Value!).ToArray();
                batches.Add(ids);
                Assert.Equal(ids.Length, intent.Take);
                return Task.FromResult(((IEnumerable<ItemsVM>)ids.Select(id => Item(id)).ToArray(), ids.Length));
            });

        Assert.True(await PurchaseClassificationItemRules.ValidateItemsAsync(handler,
            Enumerable.Range(1, 1201).Concat([1, 2]), Category, Subcategory));
        Assert.Equal(7, batches.Count);
        Assert.Equal(1201, batches.Sum(batch => batch.Length));
        Assert.All(batches, batch => Assert.InRange(batch.Length, 1, 200));
    }
}
