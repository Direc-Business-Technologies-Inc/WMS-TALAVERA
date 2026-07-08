using Shared.Entities;
using Shared.Libraries.ViewModel;
using Shared.Libraries.ViewModel.TripTicket;
using Web.BlazorServer.ViewModels.Transaction.TripTicket;

namespace Web.BlazorServer.Handlers.Repositories.Transaction.TripTicket;

public interface ITripTicketHandler
{
    Task<(IEnumerable<TripTicketDataGridVM> Data, int Count)> GetTTDataGridAsync(DataGridIntent intent, int subsidiaryId);
    Task<TripTicketVM?> GetTripTicketAsync(int id);
    Task<IEnumerable<ItemFulfillmentVM>> GetTripTicketFulfillmentsAsync(int id);
    Task<IEnumerable<ItemFulfillmentVM>> GetPackedItemFulfillmentsAsync();
    Task<IEnumerable<DriverVM>> GetDriversAsync();
    Task<IEnumerable<HelperVM>> GetHelpersAsync();
    Task<IEnumerable<LocationVM>> GetLocationsAsync();
    Task<IEnumerable<TruckPlateNumberVM>> GetTruckPlateNumbersAsync();
    Task<bool> PostTripTicketAsync(TripTicketVM data);
}
