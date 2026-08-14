using Microsoft.AspNetCore.Components;
using Web.BlazorServer.Components.Base;
using Web.BlazorServer.Components.Security;
using Web.BlazorServer.Handlers.Repositories.Transaction.InventoryTransferRequest;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Transaction.InventoryTransferRequest;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryTransferRequest;

public partial class InventoryTransferRequestCreate : BaseForm<InventoryTransferRequestVM>
{
    [Inject] IInventoryTransferRequestHandler itrHandler { get; set; } = default!;
    [Inject] AppAuthenticationService authService { get; set; } = default!;
    [Inject] IBusyDialogService BusyDialogService { get; set; } = default!;

    readonly string ActionCreate = "Create Inventory transfer request";
    bool IsBusy = false;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        AppBusyService.BusyChanged += OnBusyChanged;
    }

    protected override void OnParametersSet()
    {
        FormData.Date = DateTime.Now;
        //FormData.Memo = "Created via WMS";
        FormData.Memo = "";
        var nameClaim = authService.GetClaimValue("com.direcbusiness.wms.nsEmployeeName");
        FormData.PreparedBy = string.IsNullOrEmpty(nameClaim) ? "No Netsuite Account Registered" : nameClaim;
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

    async Task OnReturn(InventoryTransferRequestVM _)
    {
        NavManager.NavigateTo(ITRRoutes.INDEX);
    }

    async Task OnSubmit(InventoryTransferRequestVM data)
    {

        if (data.Lines.Count == 0)
        {
            ToastService.Error("Please add at least one line to the inventory transfer request.");
            return;
        }
        if (LinesNeedAssignment(data, out var lines))
        {
            ToastService.Error("Please assign inventory details to lines: " + string.Join(", ", lines.Select(l => l.ItemCode)));
            return;
        }

        if (data.Lines.Any(x => x.QuantityAlloted > x.QuantityAvailable))
        {
            ToastService.Error("Some alloted quantities exceed the available quantity");
            return;
        }

        IsBusy = true;
        await InvokeAsync(StateHasChanged);

        var action = await AppActionFactory.RunConfirmedAsync(async () =>
        {
            await itrHandler.CreateInventoryTransferRequest(data);
        }, ActionCreate);

        action.OnSuccess(() =>
        {
            NavManager.NavigateTo(ITRRoutes.INDEX);
            return Task.CompletedTask;
        });

        IsBusy = false;
        await InvokeAsync(StateHasChanged);
    }

    void OnBusyChanged(string key, bool isBusy)
    {
        if (!key.Equals(ActionCreate))
            return;

        IsBusy = isBusy;

        if (isBusy)
            BusyDialogService.Show(title: ActionCreate);
        else
            BusyDialogService.Hide();

        _ = InvokeAsync(StateHasChanged);
    }

    public override void Dispose()
    {
        AppBusyService.BusyChanged -= OnBusyChanged;
        base.Dispose();
    }

    private bool LinesNeedAssignment(InventoryTransferRequestVM data, out List<InventoryTransferRequestLineVM> lines)
    {
        lines = [..data.Lines.Where(x => !x.IsAllAssigned)];
        return lines.Any();
    }

}
