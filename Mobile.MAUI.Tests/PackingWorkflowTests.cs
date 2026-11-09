using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Mobile.MAUI.Components.Pages.Packing.Returns;
using Mobile.MAUI.Components.Pages.Packing.TransferOrder;
using Mobile.MAUI.Components.Pages.Packing.VendorReturnAuthorization;
using Mobile.MAUI.Services;
using Mobile.MAUI.States;
using Shared.Libraries.ViewModel;
using Shared.Libraries.ViewModel.Common;
using Shared.Libraries.ViewModel.TransferOrder;
using Shared.Libraries.ViewModel.Returns;
using Shared.Libraries.ViewModel.VendorReturnAuthorization;
using Xunit;

namespace Mobile.MAUI.Tests;

public sealed class PackingWorkflowTests : TestContext
{
    private readonly ApiClientService _client = new();
    private readonly DialogService _dialog;
    private readonly ToastifyService _toast = new();
    private readonly UnsaveChangesHandlerService _dirty = new();
    private readonly Queue<bool?> _confirmations = new();

    public PackingWorkflowTests()
    {
        Services.AddSingleton(_client);
        Services.AddSingleton(_toast);
        Services.AddSingleton(_dirty);
        Services.AddSingleton(new GlobalStates());
        Services.AddScoped<DialogService>();
        Services.AddScoped<BarcodeResolver>();
        Services.AddScoped<ActionFactoryService>();
        _dialog = Services.GetRequiredService<DialogService>();
        _dialog.OnOpen += (_, _, _, _) => _dialog.Close(_confirmations.Dequeue());
        _client.Respond = (url, _) => Task.FromResult<object?>(url switch
        {
            "/Packing/Returns/Items" => new List<ReturnsLineVM> { new() { NetsuiteOrderInternalId = 42, LineSequenceNumber = 1, NetsuiteMaterialInternalId = 7, LineQuantity = 20, LineQuantityBackOrdered = 2, UoMRate = 1 } },
            "/Packing/VendorReturnAuthorization/Items" => new List<VendorReturnAuthorizationLineVM> { new() { NetsuiteOrderInternalId = 42, LineSequenceNumber = 1, NetsuiteMaterialInternalId = 7, LineQuantity = 20, UoMRate = 1 } },
            "/Item/PerBarcode" => new InventoryItemVM { NetsuiteMaterialInternalId = 7, UoMRate = 1 },
            _ => null
        });
    }

    private static TransferOrderLineVM Line(decimal planned = 20) => new()
    {
        NetsuiteOrderInternalId = 42, LineSequenceNumber = 1, NetsuiteMaterialInternalId = 7,
        MaterialCode = "SKU", LineQuantity = planned, UoMRate = 1
    };

    private IRenderedComponent<TransferOrderItemView> TransferOrder()
    {
        var page = RenderComponent<TransferOrderItemView>();
        page.Instance.TestSetVendor(10);
        page.Instance.TestGood.Add(Line());
        var bad = Line(); bad.IsBad = true;
        page.Instance.TestBad.Add(bad);
        page.Instance.TestGoodMode();
        return page;
    }

    [Fact]
    public async Task Cancelled_save_then_more_scans_then_vendor_save_posts_current_quantities()
    {
        var page = TransferOrder();
        await page.InvokeAsync(() => page.Instance.TestScan("one"));
        _confirmations.Enqueue(false);
        await page.InvokeAsync(page.Instance.TestSave);
        await page.InvokeAsync(() => page.Instance.TestScan("two"));
        _confirmations.Enqueue(true);
        await page.InvokeAsync(page.Instance.TestChangeVendor);

        var post = Assert.Single(_client.Posts.Where(x => x.Url.EndsWith("/SaveScan")));
        var rows = Assert.IsType<List<TransferOrderLineVM>>(post.Body);
        Assert.Equal(2, rows.Where(x => !x.IsBad).Sum(x => x.ScannedQuantity));
        Assert.True(page.Instance.TestVendorCleared);
    }

    [Fact]
    public async Task Fresh_vendor_save_prepares_payload_without_an_ordinary_save_first()
    {
        var page = TransferOrder();
        await page.InvokeAsync(() => page.Instance.TestScan("one"));
        _confirmations.Enqueue(true);
        await page.InvokeAsync(page.Instance.TestChangeVendor);
        var rows = Assert.IsType<List<TransferOrderLineVM>>(Assert.Single(_client.Posts.Where(x => x.Url.EndsWith("/SaveScan"))).Body);
        Assert.Equal(1, rows.Single(x => !x.IsBad).ScannedQuantity);
    }

    [Fact]
    public async Task Cancelled_returns_save_keeps_display_references_and_backordered_quantity()
    {
        var page = RenderComponent<ReturnsItemView>();
        var row = Assert.Single(page.Instance.TestDisplayed);
        await page.InvokeAsync(() => page.Instance.TestScan("one"));
        _confirmations.Enqueue(false);
        await page.InvokeAsync(page.Instance.TestSave);
        Assert.Same(row, Assert.Single(page.Instance.TestRows));
        await page.InvokeAsync(() => page.Instance.TestScan("two"));
        Assert.Equal(2, row.ScannedQuantity);

        _confirmations.Enqueue(true);
        await page.InvokeAsync(page.Instance.TestSave);
        var sent = Assert.Single(Assert.IsType<List<ReturnsLineVM>>(Assert.Single(_client.Posts.Where(x => x.Url.EndsWith("/SaveScan"))).Body));
        Assert.NotSame(row, sent);
        Assert.Equal(2, sent.ScannedQuantity);
        Assert.Equal(2, sent.LineQuantityBackOrdered);
    }

    [Fact]
    public async Task Scan_and_duplicate_save_during_pending_returns_save_are_rejected()
    {
        var page = RenderComponent<ReturnsItemView>();
        await page.InvokeAsync(() => page.Instance.TestScan("one"));
        var response = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _client.Respond = (_, _) => response.Task;
        _confirmations.Enqueue(true);
        var save = page.InvokeAsync(page.Instance.TestSave);
        await page.InvokeAsync(() => page.Instance.TestScan("two"));
        await page.InvokeAsync(page.Instance.TestSave);
        Assert.Equal(1, page.Instance.TestRows[0].ScannedQuantity);
        Assert.True(_dirty.HasUnsavedChanges);
        Assert.Single(_client.Posts.Where(x => x.Url.EndsWith("/SaveScan")));
        Assert.Contains(_toast.Warnings, x => x.Contains("Scan not accepted"));
        response.SetResult(true);
        await save;
        Assert.False(_dirty.HasUnsavedChanges);
    }

    [Fact]
    public async Task Save_waits_for_operator_retry_instead_of_losing_an_inflight_barcode_lookup()
    {
        var page = RenderComponent<ReturnsItemView>();
        var lookup = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _client.Respond = (_, _) => lookup.Task;
        var scan = page.InvokeAsync(() => page.Instance.TestScan("slow"));
        await page.InvokeAsync(page.Instance.TestSave);
        Assert.DoesNotContain(_client.Posts, x => x.Url.EndsWith("/SaveScan"));
        lookup.SetResult(new InventoryItemVM { NetsuiteMaterialInternalId = 7, UoMRate = 1 });
        await scan;
        Assert.Equal(1, page.Instance.TestRows[0].ScannedQuantity);
        Assert.True(_dirty.HasUnsavedChanges);
    }

    [Fact]
    public async Task RTS_scan_during_pending_save_does_not_mutate_submitted_or_editable_quantities()
    {
        var page = RenderComponent<VendorReturnAuthorizationItemView>();
        page.Instance.TestGoodMode();
        await page.InvokeAsync(() => page.Instance.TestScan("one"));
        var response = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _client.Respond = (_, _) => response.Task;
        _confirmations.Enqueue(true);
        var save = page.InvokeAsync(page.Instance.TestSave);
        await page.InvokeAsync(() => page.Instance.TestScan("two"));
        var rows = Assert.IsType<List<VendorReturnAuthorizationLineVM>>(Assert.Single(_client.Posts.Where(x => x.Url.EndsWith("/SaveScan"))).Body);
        Assert.Equal(1, page.Instance.TestGood[0].ScannedQuantity);
        Assert.Equal(1, rows.Single(x => !x.IsBad).ScannedQuantity);
        response.SetResult(true);
        await save;
    }

    [Fact]
    public async Task Vendor_discard_clears_item_selection_as_well_as_vendor_identity()
    {
        var page = TransferOrder();
        page.Instance.TestSelect(page.Instance.TestGood[0]);
        await page.InvokeAsync(page.Instance.TestChangeVendor);
        Assert.True(page.Instance.TestVendorCleared);
    }

    [Fact]
    public async Task Refresh_preserves_both_qualities_and_blocks_save_when_the_combined_plan_shrinks()
    {
        var page = TransferOrder();
        page.Instance.TestGood[0].ScannedQuantity = 8;
        page.Instance.TestBad[0].ScannedQuantity = 6;
        _client.Respond = (_, _) => Task.FromResult<object?>(new List<TransferOrderLineVM> { Line(10) });
        await page.InvokeAsync(page.Instance.TestRefresh);
        Assert.Equal(8, page.Instance.TestGood[0].ScannedQuantity);
        Assert.Equal(6, page.Instance.TestBad[0].ScannedQuantity);
        Assert.Contains(_toast.Warnings, x => x.Contains("Scans were preserved"));
        _confirmations.Enqueue(true);
        await page.InvokeAsync(page.Instance.TestSave);
        Assert.DoesNotContain(_client.Posts, x => x.Url.EndsWith("/SaveScan"));
    }

    [Fact]
    public async Task Refresh_keeps_disappeared_scans_in_review_and_blocks_partial_save()
    {
        var page = TransferOrder();
        await page.InvokeAsync(() => page.Instance.TestScan("one"));
        _client.Respond = (_, _) => Task.FromResult<object?>(new List<TransferOrderLineVM>());
        await page.InvokeAsync(page.Instance.TestRefresh);
        Assert.Equal(1, Assert.Single(page.Instance.TestDone).ScannedQuantity);
        _confirmations.Enqueue(true);
        await page.InvokeAsync(page.Instance.TestSave);
        Assert.DoesNotContain(_client.Posts, x => x.Url.EndsWith("/SaveScan"));
    }

    [Fact]
    public async Task Failed_vendor_save_retains_current_vendor_scans_and_dirty_state()
    {
        var page = TransferOrder();
        await page.InvokeAsync(() => page.Instance.TestScan("one"));
        _client.Respond = (_, _) => throw new HttpRequestException("Connection interrupted");
        _confirmations.Enqueue(true);
        await page.InvokeAsync(page.Instance.TestChangeVendor);
        Assert.False(page.Instance.TestVendorCleared);
        Assert.Equal(1, Assert.Single(page.Instance.TestGood).ScannedQuantity);
        Assert.True(_dirty.HasUnsavedChanges);
        Assert.Contains("Connection interrupted", _toast.Errors);
    }

    [Fact]
    public async Task Failed_refresh_preserves_rows_and_allows_a_subsequent_scan()
    {
        var page = RenderComponent<ReturnsItemView>();
        await page.InvokeAsync(() => page.Instance.TestScan("one"));
        var original = page.Instance.TestRows[0];
        _client.Respond = (_, _) => throw new HttpRequestException("Items unavailable");
        await page.InvokeAsync(page.Instance.TestRefresh);
        Assert.Same(original, page.Instance.TestRows[0]);
        // Cached resolution lets the scan finish without another HTTP request.
        await page.InvokeAsync(() => page.Instance.TestScan("one"));
        Assert.Equal(2, original.ScannedQuantity);
        Assert.True(_dirty.HasUnsavedChanges);
    }

    [Fact]
    public async Task Hardware_broadcast_is_dispatched_and_held_out_of_save_until_lookup_finishes()
    {
        var page = RenderComponent<ReturnsItemView>();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var lookup = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _client.Respond = (_, _) => { entered.TrySetResult(); return lookup.Task; };
        await Task.Run(() => MauiProgram.BroadcastService.Send("hardware"));
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await page.InvokeAsync(page.Instance.TestSave);
        Assert.DoesNotContain(_client.Posts, x => x.Url.EndsWith("/SaveScan"));
        lookup.SetResult(new InventoryItemVM { NetsuiteMaterialInternalId = 7, UoMRate = 1 });
        page.WaitForAssertion(() => Assert.Equal(1, page.Instance.TestRows[0].ScannedQuantity));
        Assert.True(_dirty.HasUnsavedChanges);
    }
}
