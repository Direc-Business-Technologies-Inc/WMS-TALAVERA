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
using InventorySTRGrid = Web.BlazorServer.Components.Pages.Transaction.StockTransferRequest.Components.STRDataGrid;
using InventorySTRRow = Web.BlazorServer.ViewModels.Transaction.StockTransferRequest.StockTransferRequestDataGridVM;
using SupplierReturnRow = Web.BlazorServer.ViewModels.Transaction.SupplierReturn.SupplierReturnDataGridVM;
using TripTicketRow = Web.BlazorServer.ViewModels.Transaction.TripTicket.TripTicketDataGridVM;

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

    private IRenderedComponent<AppDataGrid<PurchaseOrderDataGridVM>> ReliabilityGrid(
        Func<DataGridIntent, Task<Web.BlazorServer.ViewModels.Abstraction.DataGridResultVM<PurchaseOrderDataGridVM>>> getter)
    {
        Services.AddSingleton(Substitute.For<IGridSettingsService>());
        return RenderComponent<AppDataGrid<PurchaseOrderDataGridVM>>(p => p
            .Add(x => x.AutoLoadGridSettings, false)
            .Add(x => x.GridSettingsLoaded, false)
            .Add(x => x.DataGetter, getter));
    }

    private static int CurrentGridPage<T>(IRenderedFragment fragment) where T : class
        => fragment.FindComponent<AppDataGrid<T>>().Instance.DataGrid!.CurrentPage;

    [Fact]
    public async Task Direct_reload_and_Radzen_load_share_latest_pending_coordination_and_truthful_completion()
    {
        var first = new TaskCompletionSource<Web.BlazorServer.ViewModels.Abstraction.DataGridResultVM<PurchaseOrderDataGridVM>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var latest = new TaskCompletionSource<Web.BlazorServer.ViewModels.Abstraction.DataGridResultVM<PurchaseOrderDataGridVM>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var cut = ReliabilityGrid(_ => ++calls == 1 ? first.Task : latest.Task);
        Task<GridRefreshOutcome> direct = null!;
        Task<GridRefreshOutcome> intermediate = null!;
        Task radzen = null!;
        await cut.InvokeAsync(() => { direct = cut.Instance.ReloadDataAsync(); });
        await cut.InvokeAsync(() => { intermediate = cut.Instance.ReloadDataAsync(); });
        await cut.InvokeAsync(() => { radzen = cut.Instance.LoadDataAsync(new LoadDataArgs { Skip = 20, Top = 20 }); });
        Assert.Equal(GridRefreshOutcome.Superseded, await intermediate);
        Assert.False(radzen.IsCompleted);
        Assert.Equal(1, calls);
        first.SetResult(Web.BlazorServer.ViewModels.Abstraction.DataGridResultVM<PurchaseOrderDataGridVM>.New([new() { ReferenceNumber = "old" }], 1));
        Assert.Equal(GridRefreshOutcome.Superseded, await direct);
        cut.WaitForAssertion(() => Assert.Equal(2, calls));
        Assert.Empty(cut.Instance.DGResult.Items);
        latest.SetResult(Web.BlazorServer.ViewModels.Abstraction.DataGridResultVM<PurchaseOrderDataGridVM>.New([new() { ReferenceNumber = "new" }], 99));
        await radzen;
        Assert.Equal("new", Assert.Single(cut.Instance.DGResult.Items).ReferenceNumber);
        Assert.Equal(99, cut.Instance.DGResult.Count);
    }

    [Fact]
    public async Task Same_scope_failure_retains_stale_rows_changed_scope_failure_does_not_and_empty_success_clears_error()
    {
        var fail = false;
        var empty = false;
        var cut = ReliabilityGrid(_ => fail
            ? Task.FromException<Web.BlazorServer.ViewModels.Abstraction.DataGridResultVM<PurchaseOrderDataGridVM>>(new InvalidOperationException("429 exhausted"))
            : Task.FromResult(Web.BlazorServer.ViewModels.Abstraction.DataGridResultVM<PurchaseOrderDataGridVM>.New(empty ? [] : [new() { ReferenceNumber = "valid" }], empty ? 0 : 1)));
        await cut.InvokeAsync(() => cut.Instance.ReloadDataAsync());
        fail = true;
        await cut.InvokeAsync(() => cut.Instance.ReloadDataAsync());
        Assert.Equal(GridRefreshOutcome.Failed, cut.Instance.LastRefreshOutcome);
        Assert.Single(cut.Instance.DGResult.Items);
        Assert.Contains("stale", cut.Instance.RefreshError);
        await cut.InvokeAsync(() => cut.Instance.LoadDataAsync(new LoadDataArgs { Skip = 10, Top = 10 }));
        Assert.Empty(cut.Instance.DGResult.Items);
        Assert.NotNull(cut.Instance.RefreshError);
        fail = false; empty = true;
        await cut.InvokeAsync(() => cut.Instance.ReloadDataAsync());
        Assert.Equal(GridRefreshOutcome.Succeeded, cut.Instance.LastRefreshOutcome);
        Assert.Null(cut.Instance.RefreshError);
        Assert.Empty(cut.Instance.DGResult.Items);
    }

    [Fact]
    public async Task Disposed_grid_does_not_publish_late_response()
    {
        var result = new TaskCompletionSource<Web.BlazorServer.ViewModels.Abstraction.DataGridResultVM<PurchaseOrderDataGridVM>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cut = ReliabilityGrid(_ => result.Task);
        Task<GridRefreshOutcome> request = null!;
        await cut.InvokeAsync(() => { request = cut.Instance.ReloadDataAsync(); });
        cut.Instance.Dispose();
        result.SetResult(Web.BlazorServer.ViewModels.Abstraction.DataGridResultVM<PurchaseOrderDataGridVM>.New([new()], 1));
        Assert.Equal(GridRefreshOutcome.Cancelled, await request);
        Assert.Empty(cut.Instance.DGResult.Items);
    }

    [Fact]
    public async Task Failed_badge_refresh_marks_retained_totals_stale_and_successful_retry_clears_marker()
    {
        var fail = false;
        var cut = RenderComponent<PrintStatusView>(p => p.Add(x => x.CountsGetter, _ => fail
            ? Task.FromException<PrintStatusCountsDTO>(new InvalidOperationException("429 exhausted"))
            : Task.FromResult(new PrintStatusCountsDTO { ForPrinting = 8, Printed = 3 })));
        fail = true;
        await cut.InvokeAsync(cut.Instance.RefreshCountsAsync);
        Assert.True(cut.Instance.CountsStale);
        Assert.Contains("8", cut.Markup);
        Assert.Contains("stale", cut.Find("[role=alert]").TextContent);
        await Assert.ThrowsAsync<InvalidOperationException>(() => cut.InvokeAsync(cut.Instance.RefreshCountsOrThrowAsync));
        fail = false;
        await cut.InvokeAsync(cut.Instance.RefreshCountsAsync);
        Assert.False(cut.Instance.CountsStale);
        Assert.Empty(cut.FindAll("[role=alert]"));
    }

    [Theory]
    [InlineData("STR")]
    [InlineData("RETURNS")]
    [InlineData("VRA")]
    public async Task Packing_status_selection_reloads_rows_without_requerying_global_badges(string source)
    {
        var returns = Substitute.For<IReturnPackingHandler>();
        var str = Substitute.For<Web.BlazorServer.Handlers.Repositories.Transaction.Packing.STR.IStockTransferRequestPackingHandler>();
        var vra = Substitute.For<Web.BlazorServer.Handlers.Repositories.Transaction.Packing.VendorReturnAuthorization.IVendorReturnAuthorizationPackingHandler>();
        var counts = 0;
        returns.GetPackingPrintStatusCounts(Arg.Any<DataGridIntent>(), Arg.Any<CancellationToken>())
            .Returns(_ => { counts++; return Task.FromResult(new PrintStatusCountsDTO { ForPrinting = 4, Printed = 2 }); });
        Services.AddSingleton(returns);
        Services.AddSingleton(str);
        Services.AddSingleton(vra);
        Services.AddSingleton(Substitute.For<IGridSettingsService>());
        Services.AddSingleton<IAppActionFactory>(new AppActionFactory(
            Substitute.For<IToastService>(), Substitute.For<IBusyService>()));
        IRenderedFragment cut = source switch
        {
            "STR" => RenderComponent<Web.BlazorServer.Components.Pages.Transaction.Packing.STR.Component.STRPackingDataGrid>(),
            "RETURNS" => RenderComponent<Web.BlazorServer.Components.Pages.Transaction.Packing.Returns.Component.ReturnsPackingDataGrid>(),
            _ => RenderComponent<Web.BlazorServer.Components.Pages.Transaction.Packing.VendorReturnAuthorization.Component.VendorReturnAuthorizationPackingDataGrid>()
        };
        cut.WaitForAssertion(() => Assert.Equal(1, counts));
        await cut.Find(".print-status-tab.printed").ClickAsync(new());
        Assert.Equal(1, counts);
    }

    [Fact]
    public async Task Shared_grid_refresh_preserves_paging_and_selection_and_uses_independent_intents()
    {
        var intents = new List<DataGridIntent>();
        var selected = new List<PurchaseOrderDataGridVM> { new() { Id = 42, ReferenceNumber = "old" } };
        var fresh = new PurchaseOrderDataGridVM { Id = 42, ReferenceNumber = "new" };
        var cut = ReliabilityGrid(intent =>
        {
            intents.Add(intent);
            return Task.FromResult(Web.BlazorServer.ViewModels.Abstraction.DataGridResultVM<PurchaseOrderDataGridVM>.New([fresh], 100));
        });
        cut.SetParametersAndRender(p => p.Add(x => x.SelectedItems, selected));
        await cut.InvokeAsync(() => cut.Instance.LoadDataAsync(new LoadDataArgs { Skip = 60, Top = 30 }));
        await cut.InvokeAsync(() => cut.Instance.ReloadDataAsync());
        Assert.All(intents, intent => { Assert.Equal(60, intent.Skip); Assert.Equal(30, intent.Take); });
        Assert.Equal(GridReadPolicy.Normal, intents[0].ReadPolicy);
        Assert.Equal(GridReadPolicy.Fresh, intents[1].ReadPolicy);
        Assert.NotSame(intents[0], intents[1]);
        Assert.Same(selected, cut.Instance.SelectedItems);
        Assert.Same(fresh, Assert.Single(selected));
    }

    [Fact]
    public async Task Inventory_STR_sync_preserves_rows_on_failure_and_count_retry_requests_fresh_totals()
    {
        Services.AddSingleton(Substitute.For<IGridSettingsService>());
        Services.AddSingleton(Substitute.For<Web.BlazorServer.Handlers.Repositories.Transaction.StockTransferRequest.IStockTransferRequestHandler>());
        Services.AddSingleton<IAppActionFactory>(new AppActionFactory(Substitute.For<IToastService>(), Substitute.For<IBusyService>()));
        var fail = false;
        var countIntents = new List<DataGridIntent>();
        Task<(IEnumerable<InventorySTRRow>, int)> Rows(DataGridIntent intent)
        {
            if (intent.Take == 1) countIntents.Add(intent.Snapshot());
            return fail
                ? Task.FromException<(IEnumerable<InventorySTRRow>, int)>(new InvalidOperationException("backend unavailable"))
                : Task.FromResult<(IEnumerable<InventorySTRRow>, int)>(([new() { Id = 42, ReferenceNumber = "STR-42" }], 8));
        }
        var cut = RenderComponent<InventorySTRGrid>(p => p.Add(x => x.DataGetter, Rows));
        cut.WaitForAssertion(() => Assert.Equal(2, countIntents.Count));
        var grid = cut.FindComponent<AppDataGrid<InventorySTRRow>>().Instance;
        fail = true;
        await cut.FindAll("button").Single(button => button.TextContent.Contains("sync")).ClickAsync(new());
        Assert.Equal(GridRefreshOutcome.Failed, grid.LastRefreshOutcome);
        Assert.Equal("STR-42", Assert.Single(grid.DGResult.Items).ReferenceNumber);
        var badges = cut.FindComponent<PrintStatusView>();
        Assert.True(badges.Instance.CountsStale);
        fail = false;
        await badges.FindAll("button").Single(button => button.TextContent == "Retry counts").ClickAsync(new());
        Assert.False(badges.Instance.CountsStale);
        Assert.Equal(6, countIntents.Count);
        Assert.All(countIntents, intent => Assert.Equal(GridReadPolicy.Fresh, intent.ReadPolicy));
        await cut.FindAll("button").Single(button => button.TextContent.Contains("sync")).ClickAsync(new());
        Assert.Equal(GridRefreshOutcome.Succeeded, grid.LastRefreshOutcome);
        Assert.Null(grid.RefreshError);

        // Changing a feature filter is a different scope: previous rows must
        // not remain under the newly selected status/date when that read fails.
        fail = true;
        await cut.Find(".print-status-tab.printed").ClickAsync(new());
        Assert.Empty(grid.DGResult.Items);
        Assert.Contains("selected scope", grid.RefreshError);
        fail = false;
        await cut.FindAll("button").Single(button => button.TextContent.Contains("sync")).ClickAsync(new());
        Assert.Single(grid.DGResult.Items);
        fail = true;
        var picker = cut.FindComponent<GridDateRange>();
        await picker.InvokeAsync(() => picker.Instance.OnApply.InvokeAsync((new DateTime(2026, 10, 1), new DateTime(2026, 10, 2))));
        Assert.Empty(grid.DGResult.Items);
        Assert.Contains("selected scope", grid.RefreshError);
    }

    [Theory]
    [InlineData("STR", true)]
    [InlineData("RETURNS", true)]
    [InlineData("VRA", true)]
    [InlineData("STR", false)]
    [InlineData("RETURNS", false)]
    [InlineData("VRA", false)]
    public async Task Packing_initialization_reads_saved_scope_once_before_loading_badges(string source, bool savedSettings)
    {
        var returns = Substitute.For<IReturnPackingHandler>();
        var str = Substitute.For<Web.BlazorServer.Handlers.Repositories.Transaction.Packing.STR.IStockTransferRequestPackingHandler>();
        var vra = Substitute.For<Web.BlazorServer.Handlers.Repositories.Transaction.Packing.VendorReturnAuthorization.IVendorReturnAuthorizationPackingHandler>();
        var intents = new List<DataGridIntent>();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var emptyRows = false;
        var nullDates = false;
        async Task Record(DataGridIntent intent)
        {
            intents.Add(intent.Snapshot());
            started.TrySetResult();
            await release.Task;
        }
        async Task<(IEnumerable<T>, int)> Rows<T>(DataGridIntent intent, Func<DateTime?, T> create)
        {
            await Record(intent);
            if (emptyRows) return (Array.Empty<T>(), 0);
            var first = new DateTime(2026, 9, 1).AddDays(intent.Skip);
            return (new[] { create(nullDates ? null : first.AddDays(2)), create(nullDates ? null : first), create(null) }, 100);
        }
        str.GetStockTransferRequestsList(Arg.Any<DataGridIntent>(), Arg.Any<int>()).Returns(call =>
            Rows(call.Arg<DataGridIntent>(), date => new Web.BlazorServer.ViewModels.Transaction.Packing.STR.StockTransferRequestPackingDataGridVM { Date = date }));
        returns.GetReturnsList(Arg.Any<DataGridIntent>(), Arg.Any<int>()).Returns(call =>
            Rows(call.Arg<DataGridIntent>(), date => new Web.BlazorServer.ViewModels.Transaction.Packing.Returns.ReturnsPackingDataGridVM { Date = date }));
        vra.GetVendorReturnAuthorizationsList(Arg.Any<DataGridIntent>(), Arg.Any<int>()).Returns(call =>
            Rows(call.Arg<DataGridIntent>(), date => new Web.BlazorServer.ViewModels.Transaction.Packing.VendorReturnAuthorization.VendorReturnAuthorizationPackingDataGridVM { Date = date }));
        var counts = 0;
        returns.GetPackingPrintStatusCounts(Arg.Any<DataGridIntent>(), Arg.Any<CancellationToken>())
            .Returns(_ => { counts++; return Task.FromResult(new PrintStatusCountsDTO { ForPrinting = 4, Printed = 2 }); });
        Services.AddSingleton(returns);
        Services.AddSingleton(str);
        Services.AddSingleton(vra);
        Services.AddScoped<IGridSettingsService, GridSettingsService>();
        Services.AddSingleton<IAppActionFactory>(new AppActionFactory(
            Substitute.For<IToastService>(), Substitute.For<IBusyService>()));
        var storage = JSInterop.Setup<string?>("window.localStorage.getItem",
            call => ((string)call.Arguments[0]!).EndsWith("-TSET"));
        IRenderedFragment cut = source switch
        {
            "STR" => RenderComponent<Web.BlazorServer.Components.Pages.Transaction.Packing.STR.Component.STRPackingDataGrid>(),
            "RETURNS" => RenderComponent<Web.BlazorServer.Components.Pages.Transaction.Packing.Returns.Component.ReturnsPackingDataGrid>(),
            _ => RenderComponent<Web.BlazorServer.Components.Pages.Transaction.Packing.VendorReturnAuthorization.Component.VendorReturnAuthorizationPackingDataGrid>()
        };
        Assert.Empty(intents);
        storage.SetResult(savedSettings ? System.Text.Json.JsonSerializer.Serialize(new DataGridSettings
        {
            PageSize = 25, CurrentPage = 2,
            Columns = new[] { new DataGridColumnSettings
            {
                Property = "ReferenceNumber", Visible = true, SortOrder = SortOrder.Descending,
                FilterValue = "saved", FilterOperator = FilterOperator.Contains
            } }
        }) : null);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(0, counts);
        release.SetResult();
        cut.WaitForAssertion(() => Assert.Equal(1, counts));
        var intent = Assert.Single(intents);
        Assert.Equal(savedSettings ? 25 : 10, intent.Take);
        Assert.Equal(savedSettings ? 50 : 0, intent.Skip);
        if (savedSettings)
        {
            Assert.Contains(intent.Sorts, sort => sort.Property == "ReferenceNumber" && sort.Direction == SortDirectionEnum.Descending);
            Assert.Contains(intent.Filters.SelectMany(filter => filter.Filters.Prepend(filter)), filter => Equals(filter.Value, "saved"));
        }
        int Page() => source switch
        {
            "STR" => CurrentGridPage<Web.BlazorServer.ViewModels.Transaction.Packing.STR.StockTransferRequestPackingDataGridVM>(cut),
            "RETURNS" => CurrentGridPage<Web.BlazorServer.ViewModels.Transaction.Packing.Returns.ReturnsPackingDataGridVM>(cut),
            _ => CurrentGridPage<Web.BlazorServer.ViewModels.Transaction.Packing.VendorReturnAuthorization.VendorReturnAuthorizationPackingDataGridVM>(cut)
        };
        Assert.Equal(savedSettings ? 2 : 0, Page());
        await cut.FindAll("button").Single(button => button.TextContent.Contains("sync")).ClickAsync(new());
        Assert.Equal(2, intents.Count);
        Assert.Equal(GridReadPolicy.Fresh, intents.Last().ReadPolicy);
        Assert.Equal(2, counts);
        Assert.Equal(savedSettings ? 2 : 0, Page());
        Assert.DoesNotContain(intent.Filters, filter => filter.Property == "Date");

        // Display dates come from this page, but are not applied to reads.
        var picker = cut.FindComponent<GridDateRange>();
        var firstDate = new DateTime(2026, 9, 1).AddDays(intent.Skip);
        Assert.Equal(firstDate, picker.Instance.StartDate);
        Assert.Equal(firstDate.AddDays(2), picker.Instance.EndDate);
        Assert.Single(picker.FindAll("button"), button => button.TextContent == "Apply");
        Assert.DoesNotContain(picker.FindAll("button"), button => button.TextContent is "Today" or "Clear");
        Assert.DoesNotContain("Source-order date", cut.Markup);
        Assert.DoesNotContain("Badges count", cut.Markup);
        await cut.InvokeAsync(async () =>
        {
            switch (source)
            {
                case "STR": await cut.FindComponent<AppDataGrid<Web.BlazorServer.ViewModels.Transaction.Packing.STR.StockTransferRequestPackingDataGridVM>>().Instance.DataGrid!.GoToPage(1); break;
                case "RETURNS": await cut.FindComponent<AppDataGrid<Web.BlazorServer.ViewModels.Transaction.Packing.Returns.ReturnsPackingDataGridVM>>().Instance.DataGrid!.GoToPage(1); break;
                default: await cut.FindComponent<AppDataGrid<Web.BlazorServer.ViewModels.Transaction.Packing.VendorReturnAuthorization.VendorReturnAuthorizationPackingDataGridVM>>().Instance.DataGrid!.GoToPage(1); break;
            }
        });
        Assert.Equal(3, intents.Count);
        Assert.DoesNotContain(intents.Last().Filters, filter => filter.Property == "Date");
        Assert.Equal(new DateTime(2026, 9, 1).AddDays(intents.Last().Skip), picker.Instance.StartDate);
        Assert.Equal(2, counts);

        emptyRows = true;
        await cut.FindAll("button").Single(button => button.TextContent.Contains("sync")).ClickAsync(new());
        Assert.Null(picker.Instance.StartDate);
        Assert.Null(picker.Instance.EndDate);
        emptyRows = false;
        nullDates = true;
        await cut.FindAll("button").Single(button => button.TextContent.Contains("sync")).ClickAsync(new());
        Assert.Null(picker.Instance.StartDate);
        Assert.Null(picker.Instance.EndDate);
        nullDates = false;
        await cut.FindAll("button").Single(button => button.TextContent.Contains("sync")).ClickAsync(new());

        var selectedStart = new DateTime(2026, 10, 1);
        var selectedEnd = new DateTime(2026, 10, 5);
        var beforeApply = intents.Count;
        var beforeApplyCounts = counts;
        await picker.InvokeAsync(() => picker.FindComponents<Radzen.Blazor.RadzenDatePicker<DateTime?>>()[0].Instance.Change.InvokeAsync(selectedStart));
        await picker.InvokeAsync(() => picker.FindComponents<Radzen.Blazor.RadzenDatePicker<DateTime?>>()[1].Instance.Change.InvokeAsync(selectedEnd));
        Assert.Equal(beforeApply, intents.Count);
        await picker.FindAll("button").Single(button => button.TextContent == "Apply").ClickAsync(new());
        Assert.Equal(beforeApply + 1, intents.Count);
        Assert.Equal(0, Page());
        Assert.Equal(beforeApplyCounts, counts);
        var start = Assert.IsType<DateTime>(Assert.Single(intents.Last().Filters,
            filter => filter.Property == "Date" && filter.ComparisonOperator == ComparisonOperatorEnum.GreaterThanOrEqual).Value);
        var end = Assert.IsType<DateTime>(Assert.Single(intents.Last().Filters,
            filter => filter.Property == "Date" && filter.ComparisonOperator == ComparisonOperatorEnum.LessThan).Value);
        Assert.Equal(selectedStart, start);
        Assert.Equal(selectedEnd.AddDays(1), end);

        await cut.Find(".print-status-tab.printed").ClickAsync(new());
        await cut.FindAll("button").Single(button => button.TextContent.Contains("sync")).ClickAsync(new());
        Assert.Equal(2, intents.Last().Filters.Count(filter => filter.Property == "Date"));
        Assert.Equal(beforeApplyCounts + 1, counts);
        // An empty filtered result must keep the operator's range visible.
        emptyRows = true;
        await cut.FindAll("button").Single(button => button.TextContent.Contains("sync")).ClickAsync(new());
        Assert.Equal(selectedStart, picker.Instance.StartDate);
        Assert.Equal(selectedEnd, picker.Instance.EndDate);
    }

    [Theory]
    [InlineData("PO")]
    [InlineData("STR")]
    [InlineData("RETURNS")]
    public async Task Receiving_initialization_reads_saved_scope_once_before_loading_badges(string source)
    {
        var handler = Substitute.For<IReceivingHandler>();
        var intents = new List<DataGridIntent>();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<(IEnumerable<T>, int)> Rows<T>(DataGridIntent intent) where T : new()
        {
            intents.Add(intent.Snapshot());
            started.TrySetResult();
            await release.Task;
            return (new[] { new T() }, 100);
        }
        handler.GetPurchaseOrderDataGridAsync(Arg.Any<DataGridIntent>()).Returns(call => Rows<PurchaseOrderDataGridVM>(call.Arg<DataGridIntent>()));
        handler.GetTransferOrderDataGridAsync(Arg.Any<DataGridIntent>()).Returns(call => Rows<TransferOrderDataGridVM>(call.Arg<DataGridIntent>()));
        handler.GetReturnsDataGridAsync(Arg.Any<DataGridIntent>()).Returns(call => Rows<ReturnsDataGridVM>(call.Arg<DataGridIntent>()));
        var counts = 0;
        handler.GetReceivingPrintStatusCounts(Arg.Any<DataGridIntent>(), Arg.Any<CancellationToken>())
            .Returns(_ => { counts++; return Task.FromResult(new PrintStatusCountsDTO { ForPrinting = 4, Printed = 2 }); });
        Services.AddSingleton(handler);
        Services.AddScoped<IGridSettingsService, GridSettingsService>();
        Services.AddSingleton<IAppActionFactory>(new AppActionFactory(Substitute.For<IToastService>(), Substitute.For<IBusyService>()));
        var storage = JSInterop.Setup<string?>("window.localStorage.getItem",
            call => ((string)call.Arguments[0]!).EndsWith("-TSET"));
        IRenderedFragment cut = source switch
        {
            "PO" => RenderComponent<PurchaseOrderGrid>(),
            "STR" => RenderComponent<TransferOrderGrid>(),
            _ => RenderComponent<ReturnsGrid>()
        };
        Assert.Empty(intents);
        storage.SetResult(System.Text.Json.JsonSerializer.Serialize(new DataGridSettings
        {
            PageSize = 25, CurrentPage = 2,
            Columns = new[] { new DataGridColumnSettings
            {
                Property = "ReferenceNumber", Visible = true, SortOrder = SortOrder.Descending,
                FilterValue = "saved", FilterOperator = FilterOperator.Contains
            } }
        }));
        try { await started.Task.WaitAsync(TimeSpan.FromSeconds(5)); }
        catch (TimeoutException)
        {
            string Error<T>() where T : class => cut.FindComponent<AppDataGrid<T>>().Instance.RefreshException?.ToString() ?? "No refresh exception";
            throw new InvalidOperationException(source switch
            {
                "PO" => Error<PurchaseOrderDataGridVM>(),
                "STR" => Error<TransferOrderDataGridVM>(),
                _ => Error<ReturnsDataGridVM>()
            });
        }
        Assert.Equal(0, counts);
        release.SetResult();
        cut.WaitForAssertion(() => Assert.Equal(1, counts));
        var intent = Assert.Single(intents);
        Assert.Equal(25, intent.Take);
        Assert.Equal(50, intent.Skip);
        Assert.Contains(intent.Sorts, sort => sort.Property == "ReferenceNumber" && sort.Direction == SortDirectionEnum.Descending);
        Assert.Contains(intent.Filters.SelectMany(filter => filter.Filters.Prepend(filter)), filter => Equals(filter.Value, "saved"));
        int Page() => source switch
        {
            "PO" => CurrentGridPage<PurchaseOrderDataGridVM>(cut),
            "STR" => CurrentGridPage<TransferOrderDataGridVM>(cut),
            _ => CurrentGridPage<ReturnsDataGridVM>(cut)
        };
        Assert.Equal(2, Page());
        await cut.FindAll("button").Single(button => button.TextContent.Contains("sync")).ClickAsync(new());
        Assert.Equal(2, intents.Count);
        Assert.Equal(GridReadPolicy.Fresh, intents.Last().ReadPolicy);
        Assert.Equal(0, intents.Last().Skip);
        Assert.Equal(0, Page());
        Assert.Equal(2, counts);
    }

    [Theory]
    [InlineData("PO")]
    [InlineData("RECEIVING_STR")]
    [InlineData("RETURNS")]
    [InlineData("INVENTORY_STR")]
    [InlineData("RTS")]
    [InlineData("TT")]
    public async Task Transaction_pickers_display_page_dates_without_filtering_until_apply(string source)
    {
        var receiving = Substitute.For<IReceivingHandler>();
        var supplier = Substitute.For<Web.BlazorServer.Handlers.Repositories.Transaction.SupplierReturn.ISupplierReturnHandler>();
        var trip = Substitute.For<Web.BlazorServer.Handlers.Repositories.Transaction.TripTicket.ITripTicketHandler>();
        Services.AddSingleton(receiving);
        Services.AddSingleton(supplier);
        Services.AddSingleton(trip);
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
                    new[] { new System.Security.Claims.Claim("Permissions", "[]") }, "test"))
            }
        });
        Services.AddSingleton(Substitute.For<Web.BlazorServer.Handlers.Repositories.Transaction.StockTransferRequest.IStockTransferRequestHandler>());
        Services.AddScoped<IGridSettingsService, GridSettingsService>();
        Services.AddSingleton<IAppActionFactory>(new AppActionFactory(Substitute.For<IToastService>(), Substitute.For<IBusyService>()));
        var rowIntents = new List<DataGridIntent>();
        var countIntents = new List<DataGridIntent>();
        var pageStart = new DateTime(2026, 10, 3);
        var empty = false;
        Task<(IEnumerable<T>, int)> Rows<T>(DataGridIntent intent, Func<DateTime, T> create)
        {
            if (intent.Take == 1)
            {
                countIntents.Add(intent.Snapshot());
                return Task.FromResult<(IEnumerable<T>, int)>((Array.Empty<T>(), 100));
            }
            rowIntents.Add(intent.Snapshot());
            return Task.FromResult<(IEnumerable<T>, int)>(empty
                ? (Array.Empty<T>(), 0)
                : (new[] { create(pageStart.AddDays(2)), create(pageStart) }, 100));
        }
        receiving.GetPurchaseOrderDataGridAsync(Arg.Any<DataGridIntent>()).Returns(call => Rows(call.Arg<DataGridIntent>(), date => new PurchaseOrderDataGridVM { Date = date }));
        receiving.GetTransferOrderDataGridAsync(Arg.Any<DataGridIntent>()).Returns(call => Rows(call.Arg<DataGridIntent>(), date => new TransferOrderDataGridVM { Date = date }));
        receiving.GetReturnsDataGridAsync(Arg.Any<DataGridIntent>()).Returns(call => Rows(call.Arg<DataGridIntent>(), date => new ReturnsDataGridVM { Date = date }));
        receiving.GetReceivingPrintStatusCounts(Arg.Any<DataGridIntent>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            countIntents.Add(call.Arg<DataGridIntent>().Snapshot());
            return Task.FromResult(new PrintStatusCountsDTO { ForPrinting = 8, Printed = 2 });
        });
        supplier.GetReturnsDataGridAsync(Arg.Any<DataGridIntent>()).Returns(call => Rows(call.Arg<DataGridIntent>(), date => new SupplierReturnRow { Date = date }));
        supplier.GetReturnStatuses(Arg.Any<DataGridIntent>()).Returns(Task.FromResult<(IEnumerable<Web.BlazorServer.ViewModels.Transaction.SupplierReturn.ReturnStatusVM>, int)>(([], 0)));
        trip.GetTTDataGridAsync(Arg.Any<DataGridIntent>(), Arg.Any<int>()).Returns(call => Rows(call.Arg<DataGridIntent>(), date => new TripTicketRow { TripDate = date }));
        IRenderedFragment cut = source switch
        {
            "PO" => RenderComponent<PurchaseOrderGrid>(),
            "RECEIVING_STR" => RenderComponent<TransferOrderGrid>(),
            "RETURNS" => RenderComponent<ReturnsGrid>(),
            "INVENTORY_STR" => RenderComponent<InventorySTRGrid>(p => p.Add(x => x.DataGetter, intent => Rows(intent, date => new InventorySTRRow { Date = date }))),
            "RTS" => RenderComponent<Web.BlazorServer.Components.Pages.Transaction.SupplierReturn.Components.SupplierReturnsDataGrid>(),
            _ => RenderComponent<Web.BlazorServer.Components.Pages.Transaction.TripTicket.Component.TripTicketDataGrid>()
        };
        var picker = cut.FindComponent<GridDateRange>();
        cut.WaitForAssertion(() =>
        {
            Assert.Equal(pageStart, picker.Instance.StartDate);
            Assert.Equal(pageStart.AddDays(2), picker.Instance.EndDate);
            Assert.NotEmpty(countIntents);
        });
        bool IsDate(AppFilterDescriptor filter) => filter.Property is "Date" or "TripDate" or "tcf.trandate";
        Assert.All(rowIntents.Concat(countIntents), intent => Assert.DoesNotContain(intent.Filters, IsDate));

        Task Navigate<T>() where T : class => cut.InvokeAsync(() => cut.FindComponent<AppDataGrid<T>>().Instance.DataGrid!.GoToPage(1));
        Task Page() => source switch
        {
            "PO" => Navigate<PurchaseOrderDataGridVM>(),
            "RECEIVING_STR" => Navigate<TransferOrderDataGridVM>(),
            "RETURNS" => Navigate<ReturnsDataGridVM>(),
            "INVENTORY_STR" => Navigate<InventorySTRRow>(),
            "RTS" => Navigate<SupplierReturnRow>(),
            _ => Navigate<TripTicketRow>()
        };
        Task Reload<T>() where T : class => cut.InvokeAsync(() => cut.FindComponent<AppDataGrid<T>>().Instance.ReloadDataAsync());
        Task Refresh() => source switch
        {
            "PO" => Reload<PurchaseOrderDataGridVM>(),
            "RECEIVING_STR" => Reload<TransferOrderDataGridVM>(),
            "RETURNS" => Reload<ReturnsDataGridVM>(),
            "INVENTORY_STR" => Reload<InventorySTRRow>(),
            "RTS" => Reload<SupplierReturnRow>(),
            _ => Reload<TripTicketRow>()
        };
        var beforePage = rowIntents.Count;
        pageStart = pageStart.AddMonths(-1);
        await Page();
        Assert.True(rowIntents.Count > beforePage);
        Assert.Equal(pageStart, picker.Instance.StartDate);
        Assert.Equal(pageStart.AddDays(2), picker.Instance.EndDate);
        Assert.DoesNotContain(rowIntents.Last().Filters, IsDate);
        empty = true;
        await Refresh();
        Assert.Null(picker.Instance.StartDate);
        Assert.Null(picker.Instance.EndDate);
        empty = false;
        await Refresh();

        var start = new DateTime(2026, 8, 1);
        var end = new DateTime(2026, 8, 31);
        var beforeEdit = rowIntents.Count;
        await picker.InvokeAsync(() => picker.FindComponents<Radzen.Blazor.RadzenDatePicker<DateTime?>>()[0].Instance.Change.InvokeAsync(start));
        await picker.InvokeAsync(() => picker.FindComponents<Radzen.Blazor.RadzenDatePicker<DateTime?>>()[1].Instance.Change.InvokeAsync(end));
        Assert.Equal(beforeEdit, rowIntents.Count);
        countIntents.Clear();
        await picker.FindAll("button").Single(button => button.TextContent == "Apply").ClickAsync(new());
        Assert.True(rowIntents.Count > beforeEdit);
        Assert.Equal(0, rowIntents.Last().Skip);
        var property = source == "TT" ? "TripDate" : "Date";
        Assert.Contains(rowIntents.Last().Filters, filter => filter.Property == property && Equals(filter.Value, start) && filter.ComparisonOperator == ComparisonOperatorEnum.GreaterThanOrEqual);
        Assert.Contains(rowIntents.Last().Filters, filter => filter.Property == property && Equals(filter.Value, end.AddDays(1)) && filter.ComparisonOperator == ComparisonOperatorEnum.LessThan);
        Assert.NotEmpty(countIntents);
        Assert.All(countIntents, intent => Assert.Equal(2, intent.Filters.Count(IsDate)));
        await Page();
        empty = true;
        await Refresh();
        Assert.Equal(start, picker.Instance.StartDate);
        Assert.Equal(end, picker.Instance.EndDate);
        Assert.Equal(2, rowIntents.Last().Filters.Count(IsDate));
    }

    [Fact]
    public async Task Feature_filters_are_captured_when_queued_rather_than_when_started()
    {
        var release = new TaskCompletionSource<Web.BlazorServer.ViewModels.Abstraction.DataGridResultVM<PurchaseOrderDataGridVM>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var observed = new List<string>();
        var scope = "first";
        var cut = ReliabilityGrid(intent =>
        {
            observed.Add((string)intent.Filters.Single().Value!);
            return observed.Count == 1 ? release.Task : Task.FromResult(new Web.BlazorServer.ViewModels.Abstraction.DataGridResultVM<PurchaseOrderDataGridVM>());
        });
        cut.SetParametersAndRender(p => p.Add(x => x.PrepareIntent, intent => intent.Filters.Add(new() { Property = "scope", Value = scope })));
        Task<GridRefreshOutcome> first = null!;
        Task<GridRefreshOutcome> pending = null!;
        await cut.InvokeAsync(() => { first = cut.Instance.ReloadDataAsync(); });
        scope = "queued";
        await cut.InvokeAsync(() => { pending = cut.Instance.ReloadDataAsync(); });
        scope = "later mutation";
        release.SetResult(new());
        await pending;
        Assert.Equal(GridRefreshOutcome.Superseded, await first);
        Assert.Equal(new[] { "first", "queued" }, observed);
    }

    [Fact]
    public async Task First_page_refresh_uses_one_fresh_read_and_updates_the_actual_Radzen_pager()
    {
        var intents = new List<DataGridIntent>();
        var cut = ReliabilityGrid(intent =>
        {
            intents.Add(intent);
            return Task.FromResult(Web.BlazorServer.ViewModels.Abstraction.DataGridResultVM<PurchaseOrderDataGridVM>.New([new() { Id = 1 }], 100));
        });
        cut.SetParametersAndRender(p => p.Add(x => x.GridSettingsLoaded, true));
        await cut.InvokeAsync(() => cut.Instance.DataGrid!.GoToPage(2));
        Assert.Equal(2, cut.Instance.DataGrid!.CurrentPage);
        var before = intents.Count;
        await cut.InvokeAsync(() => cut.Instance.ReloadFirstPageAsync());
        Assert.Equal(0, cut.Instance.DataGrid.CurrentPage);
        Assert.True(before + 1 == intents.Count, string.Join("; ", intents.Select(i => $"skip={i.Skip}, take={i.Take}, policy={i.ReadPolicy}")));
        Assert.Equal(0, intents.Last().Skip);
        Assert.Equal(GridReadPolicy.Fresh, intents.Last().ReadPolicy);
    }

    [Fact]
    public async Task A_subsidiary_change_while_loading_prevents_publication()
    {
        var release = new TaskCompletionSource<Web.BlazorServer.ViewModels.Abstraction.DataGridResultVM<PurchaseOrderDataGridVM>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cut = ReliabilityGrid(_ => release.Task);
        Task<GridRefreshOutcome> request = null!;
        await cut.InvokeAsync(() => { request = cut.Instance.ReloadDataAsync(); });
        Services.GetRequiredService<ICurrentUserService>().NsSubsidiaryId.Returns(8);
        release.SetResult(Web.BlazorServer.ViewModels.Abstraction.DataGridResultVM<PurchaseOrderDataGridVM>.New([new()], 1));
        Assert.Equal(GridRefreshOutcome.Superseded, await request);
        Assert.Empty(cut.Instance.DGResult.Items);
        Assert.Equal(GridRefreshOutcome.Superseded, cut.Instance.LastRefreshOutcome);
    }

    [Fact]
    public async Task Direct_reload_uses_current_default_filters_and_removal_overload_can_exclude_them()
    {
        var intents = new List<DataGridIntent>();
        var cut = ReliabilityGrid(intent =>
        {
            intents.Add(intent);
            return Task.FromResult(new Web.BlazorServer.ViewModels.Abstraction.DataGridResultVM<PurchaseOrderDataGridVM>());
        });
        cut.SetParametersAndRender(p => p.Add(x => x.DefaultFilters, new[] { new AppFilterDescriptor { Property = "scope", Value = "old" } }));
        await cut.InvokeAsync(() => cut.Instance.LoadDataAsync(new LoadDataArgs()));
        var current = new AppFilterDescriptor { Property = "scope", Value = "current" };
        cut.SetParametersAndRender(p => p.Add(x => x.DefaultFilters, new[] { current }));
        await cut.InvokeAsync(() => cut.Instance.ReloadDataAsync());
        Assert.Equal("old", intents[0].Filters.Single().Value);
        Assert.Equal("current", intents[1].Filters.Single().Value);
        await cut.InvokeAsync(() => cut.Instance.ReloadDataAsync(new[] { current }));
        Assert.Empty(intents[2].Filters);
    }

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
    public async Task Date_range_preserves_draft_across_parent_render_and_rejects_reversed_dates()
    {
        var applied = 0;
        var cut = RenderComponent<GridDateRange>(p => p
            .Add(x => x.OnApply, _ => applied++));
        await cut.InvokeAsync(() => cut.FindComponents<Radzen.Blazor.RadzenDatePicker<DateTime?>>()[0].Instance.Change.InvokeAsync(new DateTime(2026, 10, 1)));
        await cut.InvokeAsync(() => cut.FindComponents<Radzen.Blazor.RadzenDatePicker<DateTime?>>()[1].Instance.Change.InvokeAsync(new DateTime(2026, 10, 5)));
        cut.SetParametersAndRender(p => p.Add(x => x.StartDate, (DateTime?)null));
        await cut.FindAll("button").Single(button => button.TextContent == "Apply").ClickAsync(new());
        Assert.Equal(1, applied);
        cut.SetParametersAndRender(p => p
            .Add(x => x.StartDate, new DateTime(2026, 11, 9))
            .Add(x => x.EndDate, new DateTime(2026, 11, 8)));
        Assert.True(cut.FindAll("button").Single(button => button.TextContent == "Apply").HasAttribute("disabled"));
        Assert.Equal(1, applied);
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
