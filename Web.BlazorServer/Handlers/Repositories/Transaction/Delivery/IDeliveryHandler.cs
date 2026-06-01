using Shared.Entities;
using Web.BlazorServer.ViewModels.Transaction.Delivery;

namespace Web.BlazorServer.Handlers.Repositories.Transaction.Delivery;

public interface IDeliveryHandler
{
    Task<(IEnumerable<SalesOrderDataGridVM> Data, int Count)> GetSalesOrderDataGridAsync(DataGridIntent intent);
    Task<SalesOrderVM?> GetSalesOrderAsync(int docEntry);
    Task<(IEnumerable<DeliveryDataGridVM> Data, int Count)> GetDeliveryDataGridAsync(DataGridIntent intent);
    Task<DeliveryVM?> GetDeliveryAsync(int docEntry);
    Task<bool> PostDeliveryAsync(DeliveryVM data);
    Task<IEnumerable<DeliveryMeansVM>> GetDeliveryMeansAsync();
}
