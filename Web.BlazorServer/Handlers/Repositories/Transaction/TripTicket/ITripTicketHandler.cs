using Shared.Entities;
using Shared.Libraries.ViewModel.TripTicket;
using Web.BlazorServer.ViewModels.Transaction.TripTicket;

namespace Web.BlazorServer.Handlers.Repositories.Transaction.TripTicket;

public interface ITripTicketHandler
{
    Task<(IEnumerable<TripTicketDataGridVM> Data, int Count)> GetTTDataGridAsync(DataGridIntent intent);
    Task<TripTicketDataGridVM?> GetTripTicketAsync(int id);
    Task<bool> PostTripTicketAsync(TripTicketVM data);
}
