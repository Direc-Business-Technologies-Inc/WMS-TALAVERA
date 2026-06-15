using Microsoft.AspNetCore.Components;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Transaction.Receiving;

namespace Web.BlazorServer.Components.Pages.Transaction.Receiving.Components;

partial class PurchaseOrderDetails
{
    [Parameter]
    [EditorRequired]
    public PurchaseOrderVM Model { get; set; } = new();
}
