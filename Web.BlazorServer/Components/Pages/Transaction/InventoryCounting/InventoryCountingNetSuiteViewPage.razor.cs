using Microsoft.AspNetCore.Components;
using Shared.Kernel;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Transaction.InventoryCounting;
using InventoryCountingHeaderVM = global::Shared.Libraries.ViewModel.InventoryCounting.InventoryCountingVM;
using InventoryCountingLineVM = global::Shared.Libraries.ViewModel.InventoryCounting.InventoryCountingLineVM;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryCounting;

public partial class InventoryCountingNetSuiteViewPage
{
    [SupplyParameterFromQuery]
    [Parameter] public string? OrderNumber { get; set; }

    [Inject] IInventoryCountingHandler InventoryCountingHandler { get; set; } = default!;

    bool IsLoadingData => AppBusyService.IsBusy(ActionView);

    readonly string ActionView = EnumHelper.GetEnumDescription(AppActions.GetInventoryCountingLines);
    readonly string ActionPatch = EnumHelper.GetEnumDescription(AppActions.PatchInventoryCounting);

    InventoryCountingHeaderVM Header { get; set; } = new();
    List<InventoryCountingLineVM> Lines { get; set; } = [];

    protected override void OnInitialized()
    {
        base.OnInitialized();
        AppBusyService.SetBusy(ActionView, true);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            await LoadDataAsync();
            await InvokeAsync(StateHasChanged);
        }
    }

    async Task LoadDataAsync()
    {
        if (string.IsNullOrWhiteSpace(OrderNumber))
        {
            ToastService.Error("Please select an inventory count from the list.");
            GoBack();
            return;
        }

        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionView, true);
            return await InventoryCountingHandler.GetStartedInventoryCountingLinesAsync(OrderNumber);
        }, AppActionOptionPresets.Loading(ActionView));

        AppBusyService.SetBusy(ActionView, false);

        action.OnSuccess(result =>
        {
            Lines = result is null ? [] : [.. result];
            Header = Lines.Count > 0
                ? HeaderFromLine(Lines[0])
                : new InventoryCountingHeaderVM
                {
                    OrderNumber = OrderNumber,
                    OrderStatus = "B",
                    OrderType = "inventorycount"
                };

            return Task.CompletedTask;
        });
    }

    async Task PatchLinesAsync()
    {
        if (Lines.Any(line => line.ScannedQuantity < 0))
        {
            ToastService.Warning("GOOD quantity cannot be negative.");
            return;
        }

        List<InventoryCountingLineVM> linesToPatch =
        [
            .. Lines.Where(line => line.ScannedQuantity > 0)
        ];

        if (linesToPatch.Count == 0)
        {
            ToastService.Warning("Please enter a GOOD quantity for at least one line.");
            return;
        }

        if (linesToPatch.Any(line => line.NetsuiteInventoryDetailInternalId is null))
        {
            ToastService.Warning("One or more lines have no NetSuite inventory detail reference.");
            return;
        }

        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionPatch, true);
            return await InventoryCountingHandler.PatchStartedInventoryCountingAsync(linesToPatch);
        }, AppActionOptionPresets.Confirmed(ActionPatch));

        AppBusyService.SetBusy(ActionPatch, false);

        action.OnSuccess(result =>
        {
            if (!result)
            {
                ToastService.Error("Failed to patch inventory count.");
                return Task.CompletedTask;
            }

            ToastService.Success("Inventory count patched successfully.");
            NavManager.NavigateTo("/transactions/inventory/inventory-counting", true);
            return Task.CompletedTask;
        });
    }

    static InventoryCountingHeaderVM HeaderFromLine(InventoryCountingLineVM line) =>
        new()
        {
            NetsuiteOrderInternalId = line.NetsuiteOrderInternalId,
            OrderNumber = line.OrderNumber,
            OrderType = line.OrderType,
            OrderStatus = line.OrderStatus,
            NetsuiteOrderCreatedDate = line.NetsuiteOrderCreatedDate
        };

    void GoBack() =>
        NavManager.NavigateTo("/transactions/inventory/inventory-counting", true);
}
