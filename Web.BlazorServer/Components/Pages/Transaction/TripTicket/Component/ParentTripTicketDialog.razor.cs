using Application.DataTransferObjects.Transactions.TripTicket;
using Microsoft.AspNetCore.Components;
using Shared.Entities;
using Shared.Libraries.Utilities;
using Web.BlazorServer.Handlers.Repositories.Transaction.TripTicket;
using Web.BlazorServer.ViewModels.Transaction.TripTicket;

namespace Web.BlazorServer.Components.Pages.Transaction.TripTicket.Component;

public partial class ParentTripTicketDialog
{
    [Inject] ITripTicketHandler triptickethandler { get; set; } = default!;

    readonly string ActionGetList = "Get Trip Tickets";

    async Task<(IEnumerable<TripTicketDataGridVM>, int)> TripTicketsProvider(DataGridIntent intent)
    {
        intent.Sorts.Add(DataGridSortUtilities.Descending(nameof(TripTicketDataGridVM.TripDate)));
        return await triptickethandler.GetParentTripTicketsAsync(intent);
    }

    async Task SelectTT(TripTicketDataGridVM item)
    {
        DialogService.Close(item.NetsuiteTripTicketInternalId);
    }
}