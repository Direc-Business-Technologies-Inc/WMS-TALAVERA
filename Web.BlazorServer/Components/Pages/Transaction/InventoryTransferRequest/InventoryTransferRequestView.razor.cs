using Mapster;
using Microsoft.AspNetCore.Components;
using Web.BlazorServer.Components.Base;
using Web.BlazorServer.Components.Pages.Transaction.StockTransferRequest;
using Web.BlazorServer.Handlers.Repositories.Transaction.InventoryTransfer;
using Web.BlazorServer.Handlers.Repositories.Transaction.InventoryTransferRequest;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.ViewModels.Transaction.InventoryTransferRequest;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryTransferRequest;

public partial class InventoryTransferRequestView : BaseForm<InventoryTransferRequestVM>
{
    [Inject] IInventoryTransferRequestHandler itrHandler { get; set; } = default!;
    [SupplyParameterFromQuery] public string? Ref { get; set; } = null;

    bool IsLoadingData => AppBusyService.IsBusy(ActionGet);
    readonly string ActionGet = "Get Inventory transfer request";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            AppBusyService.SetBusy(ActionGet, true);
            await InvokeAsync(StateHasChanged);
            await LoadDataAsync();
        }
    }

    async Task LoadDataAsync()
    {
        var action = await AppActionFactory.RunLoadingAsync(async () =>
        {
            if (Ref is null) throw new Exception("Please select an inventory transfer request");

            return await itrHandler.GetInventoryTransferRequestAsync(Ref);
        }, ActionGet);

        action.OnSuccess(async (res) =>
        {
            res.Adapt(FormData);
            await InvokeAsync(StateHasChanged);
        });

        action.OnFailure(ex =>
        {
            return Task.Delay(100).ContinueWith(_ =>
            {
                NavManager.NavigateTo(ITRRoutes.INDEX);
            });
        });
    }

    async Task OnReturn(InventoryTransferRequestVM data)
    {
        NavManager.NavigateTo(ITRRoutes.INDEX);
    }

    protected override Task CancelEditing()
    {
        throw new NotImplementedException();
    }

    protected override Task HandleSubmit()
    {
        throw new NotImplementedException();
    }

    protected override Task InitializeEditing()
    {
        throw new NotImplementedException();
    }
}
