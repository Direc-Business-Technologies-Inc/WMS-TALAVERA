using Application.DataTransferObjects.Transactions.ItemFulfillment;
using Application.DataTransferObjects.Transactions.TripTicket;
using Application.UseCases.Repositories.Integration.Transaction.TripTicket;
using Shared.Entities;

namespace Integration.NS.Implementations.Others.Caching;

/// <summary>
/// 60-second grid cache for trip ticket lists (Req 2.1). Trip tickets are
/// read-only in NetSuite (no mutation methods on the integration), so only the
/// paginated grid reads are cached and there is nothing to evict.
/// </summary>
public class CachedTripTicketIntegration(
    Transactions.TripTicketIntegration inner,
    GridResultCache cache) : ITripTicketIntegration
{
    public const string EntityName = "TripTicket";

    public Task<(IEnumerable<TripTicketDataGridDTO> Data, int Count)> GetTripTicketsAsync(DataGridIntent intent, int subsidiaryId)
        => cache.GetOrAddAsync(EntityName, nameof(GetTripTicketsAsync), intent,
            () => inner.GetTripTicketsAsync(intent, subsidiaryId), default, subsidiaryId);

    public Task<(IEnumerable<TripTicketDataGridDTO> Data, int Count)> GetParentTripTicketsAsync(DataGridIntent intent)
        => cache.GetOrAddAsync(EntityName, nameof(GetParentTripTicketsAsync), intent,
            () => inner.GetParentTripTicketsAsync(intent));

    public Task<(IEnumerable<ItemFulfillmentDTO>, int)> GetItemfulfillments(DataGridIntent intent)
        => inner.GetItemfulfillments(intent);

    public Task<TripTicketDataGridDTO?> GetTripTicketAsync(int id)
        => inner.GetTripTicketAsync(id);

    public Task<TripTicketDataGridDTO?> GetParentTripTicketAsync(int id)
        => inner.GetParentTripTicketAsync(id);

    public Task<IEnumerable<TripTicketFulfillmentDTO>> GetTripTicketFulfillmentsAsync(int id)
        => inner.GetTripTicketFulfillmentsAsync(id);

    public Task<IEnumerable<TripTicketFulfillmentDTO>> GetTripTicketFulfillmentsBaseParentAsync(int id)
        => inner.GetTripTicketFulfillmentsBaseParentAsync(id);
}