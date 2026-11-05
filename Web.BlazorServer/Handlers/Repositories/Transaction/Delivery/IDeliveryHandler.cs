using Shared.Entities;
using Web.BlazorServer.ViewModels.Transaction.Delivery;

namespace Web.BlazorServer.Handlers.Repositories.Transaction.Delivery;

public interface IDeliveryHandler
{
    Task<(IEnumerable<SalesOrderDataGridVM> Data, int Count)> GetSalesOrderDataGridAsync(DataGridIntent intent, CancellationToken ct = default);
    Task<SalesOrderVM?> GetSalesOrderAsync(int docEntry, CancellationToken ct = default);
    Task<(IEnumerable<DeliveryDataGridVM> Data, int Count)> GetDeliveryDataGridAsync(DataGridIntent intent, CancellationToken ct = default);
    Task<DeliveryVM?> GetDeliveryAsync(int docEntry, CancellationToken ct = default);
    Task<bool> PostDeliveryAsync(DeliveryVM data, CancellationToken ct = default);
    Task<IEnumerable<DeliveryMeansVM>> GetDeliveryMeansAsync(CancellationToken ct = default);
}
