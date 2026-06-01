using Application.DataTransferObjects.Transactions.Delivery;
using Application.DataTransferObjects.Transactions.Delivery.SAP;
using Shared.Entities;

namespace Application.UseCases.Repositories.Integration.Transaction.Delivery;

public interface IDeliveryIntegration
{
    Task<IEnumerable<DeliveryMeansSAPDTO>> GetDeliveryMeansAsync();
    Task<(IEnumerable<DeliveryDataGridSAPDTO> Data, int Count)> GetDeliveryDocumentsAsync(DataGridIntent intent);
    Task<DeliveryHeaderSAPDTO?> GetDeliveryDocumentHeaderAsync(int docEntry);
    Task<IEnumerable<DeliveryLineSAPDTO>> GetDeliveryDocumentLinesAsync(int docEntry);
    Task<(IEnumerable<SalesOrderDataGridSAPDTO> Data, int Count)> GetSalesOrderDocumentsAsync(DataGridIntent intent);
    Task<SalesOrderHeaderSAPDTO?> GetSalesOrderDocumentHeaderAsync(int docEntry);
    Task<IEnumerable<SalesOrderLineSAPDTO>> GetSalesOrderDocumentLinesAsync(int docEntry);
    Task<bool> PostDeliveryDocument(DeliveryDTO document);
}
