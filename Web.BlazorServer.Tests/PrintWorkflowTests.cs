using Application.UseCases.Repositories.Integration.Others;
using Application.DataTransferObjects.Transactions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Radzen;
using Shared.Entities;
using Shared.Services.Repository;
using Web.BlazorServer.Components.Custom;
using Web.BlazorServer.Components.Pages.Transaction.Packing.Components;
using Web.BlazorServer.Components.Pages.Transaction.Receiving.Components;
using Web.BlazorServer.Components.Security;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Handlers.Repositories.Transaction.Packing.Returns;
using Web.BlazorServer.Handlers.Repositories.Transaction.Receiving;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.Services.Implementation;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Transaction.Packing;
using Web.BlazorServer.ViewModels.Transaction.Receiving;
using Xunit;

namespace Web.BlazorServer.Tests;

public class PrintWorkflowTests : TestContext
{
    private readonly INetSuiteApiClientService client = Substitute.For<INetSuiteApiClientService>();
    private readonly NotificationService notifications = new();

    public PrintWorkflowTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddSingleton(client);
        Services.AddSingleton(notifications);
        Services.AddScoped<DialogService>();
        Services.AddScoped<TooltipService>();
        Services.AddScoped<ContextMenuService>();
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor());
        Services.AddScoped<AppAuthenticationService>();
        Services.AddScoped<AuthorizationHelper>();
        Services.AddScoped<UnsavedChangesService>();
        Services.AddSingleton(Substitute.For<IAppActionFactory>());
        Services.AddSingleton(Substitute.For<IBusyService>());
        Services.AddSingleton(Substitute.For<IToastService>());
        Services.AddSingleton(Substitute.For<IAlertService>());
        var user = Substitute.For<ICurrentUserService>();
        user.NsSubsidiaryId.Returns(7);
        Services.AddSingleton(user);
    }

    private IRenderedComponent<AppPrintLink> PrintLink(Func<Task>? onPrint = null, string recordType = "customrecord_dbti_trip_ticket")
        => RenderComponent<AppPrintLink>(p => p
            .Add(x => x.Url, "https://example.test/print/42")
            .Add(x => x.RecordType, recordType)
            .Add(x => x.RecordId, 42)
            .Add(x => x.OnPrint, onPrint ?? (() => Task.CompletedTask)));

    [Fact]
    public async Task Trip_ticket_click_updates_counter_and_refreshes_without_waiting_for_browser_print()
    {
        var refreshed = 0;
        var cut = PrintLink(() => { refreshed++; return Task.CompletedTask; });
        Assert.Equal("_blank", cut.Find("a").GetAttribute("target"));

        await cut.Find(".print-link-action").ClickAsync(new());

        await client.Received(1).IncrementTripTicketPrintCountAsync(42);
        Assert.Equal(1, refreshed);
        Assert.Empty(notifications.Messages);
    }

    [Fact]
    public async Task Refresh_failure_after_successful_write_is_not_reported_as_counter_failure()
    {
        var cut = PrintLink(() => Task.FromException(new InvalidOperationException("refresh failed")));
        await cut.Find(".print-link-action").ClickAsync(new());

        var message = Assert.Single(notifications.Messages);
        Assert.Equal(NotificationSeverity.Warning, message.Severity);
        Assert.Equal("Refresh needed", message.Summary);
        Assert.Contains("Print count updated", message.Detail);
        await client.Received(1).IncrementTripTicketPrintCountAsync(42);
    }

    [Fact]
    public async Task Failed_write_does_not_refresh_or_claim_success()
    {
        client.IncrementTripTicketPrintCountAsync(42).Returns(Task.FromException(new InvalidOperationException("NetSuite rejected write")));
        var refreshed = 0;
        var cut = PrintLink(() => { refreshed++; return Task.CompletedTask; });
        await cut.Find(".print-link-action").ClickAsync(new());

        Assert.Equal(0, refreshed);
        Assert.Equal(NotificationSeverity.Error, Assert.Single(notifications.Messages).Severity);
        Assert.False(cut.Find("button").HasAttribute("disabled"));
    }

    [Fact]
    public async Task Busy_print_link_does_not_submit_a_second_update()
    {
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        client.IncrementTripTicketPrintCountAsync(42).Returns(pending.Task);
        var cut = PrintLink();
        var firstClick = cut.Find(".print-link-action").ClickAsync(new());
        cut.WaitForAssertion(() => Assert.True(cut.Find("button").HasAttribute("disabled")));
        await cut.Find(".print-link-action").ClickAsync(new());
        await client.Received(1).IncrementTripTicketPrintCountAsync(42);
        pending.SetResult();
        await firstClick;
    }

    [Theory]
    [InlineData("itemReceipt")]
    [InlineData("itemFulfillment")]
    [InlineData("vendorReturnAuthorization")]
    public async Task Receipt_label_and_RTS_clicks_invoke_parent_refresh(string recordType)
    {
        var refreshed = 0;
        var cut = PrintLink(() => { refreshed++; return Task.CompletedTask; }, recordType);
        await cut.Find(".print-link-action").ClickAsync(new());
        await client.Received(1).IncrementPrintCountAsync(recordType, 42);
        Assert.Equal(1, refreshed);
    }

    [Theory]
    [InlineData("STR")]
    [InlineData("RETURNS")]
    [InlineData("VRA")]
    public async Task Packing_modal_shows_initial_loading_then_refreshes_itself_and_parent_after_print(string source)
    {
        var handler = Substitute.For<IReturnPackingHandler>();
        var pending = new TaskCompletionSource<(IEnumerable<PackedItemFulfillmentVM>, int)>(TaskCreationOptions.RunContinuationsAsynchronously);
        handler.GetPackedItemFulfillments(Arg.Any<DataGridIntent>()).Returns(pending.Task);
        Services.AddSingleton(handler);
        var refreshed = 0;
        var closed = false;
        Services.GetRequiredService<DialogService>().OnClose += _ => closed = true;
        var cut = RenderComponent<PackingLabelSelectionDialog>(p => p
            .Add(x => x.TransactionId, 100).Add(x => x.Source, source)
            .Add(x => x.OnPrinted, () => { refreshed++; }));
        Assert.Contains("Loading Packing Labels", cut.Markup);
        pending.SetResult((new[] { new PackedItemFulfillmentVM { Id = 42, ReferenceNumber = "IF-42" } }, 1));
        cut.WaitForAssertion(() => Assert.Contains("IF-42", cut.Markup));
        handler.GetPackedItemFulfillments(Arg.Any<DataGridIntent>()).Returns(Task.FromResult<(IEnumerable<PackedItemFulfillmentVM>, int)>((Array.Empty<PackedItemFulfillmentVM>(), 0)));

        await cut.InvokeAsync(() => cut.FindComponent<AppPrintLink>().Instance.OnPrint.InvokeAsync());

        Assert.Equal(1, refreshed);
        Assert.False(closed);
        Assert.DoesNotContain("IF-42", cut.Markup);
        Assert.Contains("No packing labels match", cut.Markup);
        await handler.Received(2).GetPackedItemFulfillments(Arg.Any<DataGridIntent>());
    }

    [Fact]
    public async Task Receiving_modal_shows_loading_and_reloads_without_closing_after_print()
    {
        var handler = Substitute.For<IReceivingHandler>();
        var pending = new TaskCompletionSource<(IEnumerable<ItemReceiptDataGridVM>, int)>(TaskCreationOptions.RunContinuationsAsynchronously);
        handler.GetItemReceiptsDatagridAsync(Arg.Any<DataGridIntent>()).Returns(pending.Task);
        Services.AddSingleton(handler);
        var refreshed = 0;
        var cut = RenderComponent<ReceivingReportSelectionDialog>(p => p
            .Add(x => x.TransactionId, 100).Add(x => x.OnPrinted, () => { refreshed++; }));
        Assert.Contains("Loading Receiving Reports", cut.Markup);
        pending.SetResult((new[] { new ItemReceiptDataGridVM { Id = 42, ReferenceNumber = "RR-42" } }, 1));
        cut.WaitForAssertion(() => Assert.Contains("RR-42", cut.Markup));
        handler.GetItemReceiptsDatagridAsync(Arg.Any<DataGridIntent>()).Returns(Task.FromResult<(IEnumerable<ItemReceiptDataGridVM>, int)>((Array.Empty<ItemReceiptDataGridVM>(), 0)));

        await cut.InvokeAsync(() => cut.FindComponent<AppPrintLink>().Instance.OnPrint.InvokeAsync());

        Assert.Equal(1, refreshed);
        Assert.DoesNotContain("RR-42", cut.Markup);
        Assert.Contains("No receiving reports match", cut.Markup);
        await handler.Received(2).GetItemReceiptsDatagridAsync(Arg.Any<DataGridIntent>());
    }

    [Fact]
    public void Date_range_is_empty_until_operator_applies_dates()
    {
        var cut = RenderComponent<GridDateRange>();
        Assert.Null(cut.Instance.StartDate);
        Assert.Null(cut.Instance.EndDate);
    }

    [Fact]
    public async Task Badge_refresh_retains_previous_totals_with_an_updating_indicator()
    {
        var pending = new TaskCompletionSource<PrintStatusCountsDTO>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var cut = RenderComponent<PrintStatusView>(p => p.Add(x => x.CountsGetter, _ =>
            ++calls == 1 ? Task.FromResult(new PrintStatusCountsDTO { ForPrinting = 8, Printed = 3 }) : pending.Task));
        Task? refresh = null;
        await cut.InvokeAsync(() => { refresh = cut.Instance.RefreshCountsAsync(); });

        Assert.Contains("Updating counts", cut.Markup);
        Assert.Equal(new[] { "8", "3" }, cut.FindAll("strong").Select(x => x.TextContent));
        pending.SetResult(new PrintStatusCountsDTO { ForPrinting = 7, Printed = 4 });
        await refresh!;
        cut.WaitForAssertion(() => Assert.Equal(new[] { "7", "4" }, cut.FindAll("strong").Select(x => x.TextContent)));
    }

    [Fact]
    public async Task Rapid_badge_refreshes_cancel_old_work_and_coalesce_to_the_latest_request()
    {
        var first = new TaskCompletionSource<PrintStatusCountsDTO>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken firstToken = default;
        var calls = 0;
        var cut = RenderComponent<PrintStatusView>(p => p.Add(x => x.CountsGetter, token =>
        {
            if (++calls == 1) { firstToken = token; return first.Task; }
            return Task.FromResult(new PrintStatusCountsDTO { ForPrinting = 5, Printed = 6 });
        }));
        Task? refresh = null;
        await cut.InvokeAsync(() =>
        {
            refresh = cut.Instance.RefreshCountsAsync();
            _ = cut.Instance.RefreshCountsAsync();
        });
        Assert.True(firstToken.IsCancellationRequested);
        first.SetResult(new PrintStatusCountsDTO { ForPrinting = 999, Printed = 999 });
        await refresh!;

        Assert.Equal(2, calls);
        cut.WaitForAssertion(() => Assert.Equal(new[] { "5", "6" }, cut.FindAll("strong").Select(x => x.TextContent)));
    }

    [Fact]
    public async Task Disposing_badges_cancels_inflight_work()
    {
        CancellationToken token = default;
        var pending = new TaskCompletionSource<PrintStatusCountsDTO>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cut = RenderComponent<PrintStatusView>(p => p.Add(x => x.CountsGetter, ct =>
        {
            token = ct;
            return pending.Task.WaitAsync(ct);
        }));
        cut.WaitForAssertion(() => Assert.True(token.CanBeCanceled));
        await cut.InvokeAsync(() => cut.Instance.Dispose());
        Assert.True(token.IsCancellationRequested);
    }

    [Fact]
    public async Task Modal_load_failure_has_retry_feedback_and_can_recover()
    {
        var handler = Substitute.For<IReceivingHandler>();
        handler.GetItemReceiptsDatagridAsync(Arg.Any<DataGridIntent>())
            .Returns(Task.FromException<(IEnumerable<ItemReceiptDataGridVM>, int)>(new InvalidOperationException("read failed")));
        Services.AddSingleton(handler);
        var cut = RenderComponent<ReceivingReportSelectionDialog>(p => p.Add(x => x.TransactionId, 100));
        Assert.Contains("could not be loaded", cut.Find("[role=alert]").TextContent);

        handler.GetItemReceiptsDatagridAsync(Arg.Any<DataGridIntent>()).Returns(
            Task.FromResult<(IEnumerable<ItemReceiptDataGridVM>, int)>((new[] { new ItemReceiptDataGridVM { Id = 42, ReferenceNumber = "RR-42" } }, 1)));
        await cut.FindAll("button").Single(button => button.TextContent.Contains("Refresh")).ClickAsync(new());

        Assert.Empty(cut.FindAll("[role=alert]"));
        Assert.Contains("RR-42", cut.Markup);
    }

    [Fact]
    public async Task Overlapping_modal_print_callbacks_do_not_drop_the_later_refresh()
    {
        var handler = Substitute.For<IReceivingHandler>();
        var pending = new TaskCompletionSource<(IEnumerable<ItemReceiptDataGridVM>, int)>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        handler.GetItemReceiptsDatagridAsync(Arg.Any<DataGridIntent>()).Returns(_ =>
        {
            calls++;
            if (calls == 2) return pending.Task;
            return Task.FromResult<(IEnumerable<ItemReceiptDataGridVM>, int)>(calls == 1
                ? (new[] { new ItemReceiptDataGridVM { Id = 42, ReferenceNumber = "RR-42" } }, 1)
                : (Array.Empty<ItemReceiptDataGridVM>(), 0));
        });
        Services.AddSingleton(handler);
        var cut = RenderComponent<ReceivingReportSelectionDialog>(p => p.Add(x => x.TransactionId, 100));
        var callback = cut.FindComponent<AppPrintLink>().Instance.OnPrint;
        var firstRefresh = cut.InvokeAsync(() => callback.InvokeAsync());
        cut.WaitForAssertion(() => Assert.Equal(2, calls));
        var laterRefresh = cut.InvokeAsync(() => callback.InvokeAsync());
        pending.SetResult((new[] { new ItemReceiptDataGridVM { Id = 42, ReferenceNumber = "RR-42" } }, 1));
        await Task.WhenAll(firstRefresh, laterRefresh);

        Assert.Equal(3, calls);
        Assert.DoesNotContain("RR-42", cut.Markup);
    }
}
