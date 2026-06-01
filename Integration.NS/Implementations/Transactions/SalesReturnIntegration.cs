using Application.DataTransferObjects.Transactions.SalesReturn;
using Application.DataTransferObjects.Transactions.SalesReturn.SAP;
using Application.UseCases.Repositories.Integration.Transaction.SalesReturn;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.Implementations.Transactions;

public class SalesReturnIntegration : ISalesReturnIntegration
{
    public Task<IEnumerable<ReturnTypeSAPDTO>> GetReturnTypesAsync()
    {
        throw new NotImplementedException();
    }

    public Task<(IEnumerable<SalesReturnDataGridSAPDTO> Data, int Count)> GetSalesReturnDataAsync(DataGridIntent intent)
    {
        throw new NotImplementedException();
    }

    public Task<SalesReturnHeaderSAPDTO?> GetSalesReturnHeaderAsync(int docEntry)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<SalesReturnLinesSAPDTO>> GetSalesReturnLinesAsync(int docEntry)
    {
        throw new NotImplementedException();
    }

    public Task<(IEnumerable<SalesReturnRequestDataGridSAPDTO> Data, int Count)> GetSalesReturnRequestDataAsync(DataGridIntent intent)
    {
        throw new NotImplementedException();
    }

    public Task<SalesReturnRequestHeaderSAPDTO?> GetSalesReturnRequestHeaderAsync(int docEntry)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<SalesReturnRequestLinesSAPDTO>> GetSalesReturnRequestLinesAsync(int docEntry)
    {
        throw new NotImplementedException();
    }

    public Task<bool> PostSalesReturnAsync(SalesReturnDTO data)
    {
        throw new NotImplementedException();
    }

    public Task<bool> PostSalesReturnFromDeliveryAsync(SalesReturnDTO data)
    {
        throw new NotImplementedException();
    }

    public Task<bool> PostSalesReturnFromRequestAsync(SalesReturnDTO data)
    {
        throw new NotImplementedException();
    }
}
