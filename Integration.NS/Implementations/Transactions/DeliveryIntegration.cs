using Application.DataTransferObjects.Transactions.Delivery;
using Application.DataTransferObjects.Transactions.Delivery.SAP;
using Application.UseCases.Repositories.Integration.Transaction.Delivery;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.Implementations.Transactions;

public class DeliveryIntegration : IDeliveryIntegration
{
    public Task<DeliveryHeaderSAPDTO?> GetDeliveryDocumentHeaderAsync(int docEntry)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<DeliveryLineSAPDTO>> GetDeliveryDocumentLinesAsync(int docEntry)
    {
        throw new NotImplementedException();
    }

    public Task<(IEnumerable<DeliveryDataGridSAPDTO> Data, int Count)> GetDeliveryDocumentsAsync(DataGridIntent intent)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<DeliveryMeansSAPDTO>> GetDeliveryMeansAsync()
    {
        throw new NotImplementedException();
    }

    public Task<SalesOrderHeaderSAPDTO?> GetSalesOrderDocumentHeaderAsync(int docEntry)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<SalesOrderLineSAPDTO>> GetSalesOrderDocumentLinesAsync(int docEntry)
    {
        throw new NotImplementedException();
    }

    public Task<(IEnumerable<SalesOrderDataGridSAPDTO> Data, int Count)> GetSalesOrderDocumentsAsync(DataGridIntent intent)
    {
        throw new NotImplementedException();
    }

    public Task<bool> PostDeliveryDocument(DeliveryDTO document)
    {
        throw new NotImplementedException();
    }
}
