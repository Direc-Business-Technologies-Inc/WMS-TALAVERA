using Microsoft.AspNetCore.Components;
using Web.BlazorServer.Components.Pages.Transaction.InventoryAdjustment.Components.InventoryAdjustmentForm;
using Web.BlazorServer.Components.Security;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.Handlers.Repositories.Transaction.InventoryAdjustment;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Transaction.InventoryAdjustment;

namespace Web.BlazorServer.Components.Pages.Transaction.InventoryAdjustment;

public partial class InventoryAdjustmentCreatePage
{
    [Inject] public IInventoryAdjustmentHandler? inventoryAdjustmentHandler { get; set; }
    [Inject] public AppAuthenticationService authService { get; set; } = default!;
    [Inject] public IBusyDialogService BusyDialogService { get; set; } = default!;
    [Inject] public ISubsidiaryHandler subsidiaryHandler { get; set; } = default!;
    [Inject] public IHttpContextAccessor httpContextAccessor { get; set; } = default!;
    [SupplyParameterFromQuery] public string? Type { get; set; }
    bool IsIssue => Type is not null && Type.Equals("issue", StringComparison.OrdinalIgnoreCase);
    bool IsBusy = false;
    readonly string ActionCreateInventoryAdjustment = "Create Inventory Adjustment";

    InventoryAdjustmentForm? Form { get; set; }

    List<ViewModels.System.NavigationRouteVM> AdditionalRoutes { get; set; } = [new() {
        Name = "Inventory Adjustment Document",
        Position = 0,
        Icon = "assignment_add",
    }];

    protected override void OnInitialized()
    {
        base.OnInitialized();
    }

    async Task OnSubmit(InventoryAdjustmentVM data)
    {
        IsBusy = true;
        BusyDialogService.Show(title: ActionCreateInventoryAdjustment);
        await InvokeAsync(StateHasChanged);

        var action = await AppActionFactory.RunConfirmedAsync(async () =>
        {
            if (inventoryAdjustmentHandler is null) throw new Exception("No registered handler for inventory adjustment");

            await inventoryAdjustmentHandler.CreateInventoryAdjustmentAsync(data);
        }, ActionCreateInventoryAdjustment);

        action.OnSuccess(async () =>
        {
            await Task.Delay(100);
            NavManager.NavigateTo(InventoryAdjustmentRoutes.INDEX);
        });

        BusyDialogService.Hide();
        IsBusy = false;
        await InvokeAsync(StateHasChanged);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            IsBusy = true;
            await InvokeAsync(StateHasChanged);

            FormData.Date = DateTime.Now;
            FormData.Memo = "";
            var nsEmployee = authService.GetClaimValue("com.direcbusiness.wms.nsEmployeeName");
            FormData.PreparedBy = string.IsNullOrEmpty(nsEmployee) ? "No Netsuite Account Registered" : nsEmployee;
            FormData.Category = new()
            {
                Name = IsIssue ? "Goods Issue" : "Goods Receipt",
                Id = -1
            };
            AdditionalRoutes[0].Name = IsIssue ? "Goods Issue" : "Goods Receipt";


            string? subsidiaryId = httpContextAccessor.HttpContext?.User?.FindFirst("com.direcbusiness.wms.nsSubsidiary")?.Value;
            if (!string.IsNullOrEmpty(subsidiaryId) && int.TryParse(subsidiaryId, out int subId))
            {
                //FormData.Subsidiary = ;
                var subsidiary = await subsidiaryHandler.GetSubsidiaryAsync(subId);
                if (Form is null)
                    FormData.Subsidiary = subsidiary;
                else
                    await Form.SetSubsidiary(subsidiary);

                IsBusy = false;
                await InvokeAsync(StateHasChanged);
            }
        }
    }
    Task OnReturn(InventoryAdjustmentVM data)
    {
        NavManager.NavigateTo(InventoryAdjustmentRoutes.INDEX);
        return Task.CompletedTask;
    }

    protected override Task InitializeEditing()
    {
        throw new NotImplementedException();
    }

    protected override Task CancelEditing()
    {
        throw new NotImplementedException();
    }

    protected override Task HandleSubmit()
    {
        throw new NotImplementedException();
    }
}
