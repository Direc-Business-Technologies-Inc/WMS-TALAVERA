using Application.DataTransferObjects.Transactions.GoodsReturn;
using Application.DataTransferObjects.Transactions.GoodsReturn.SAP;
using Application.UseCases.Repositories.Integration.Transaction.GoodsReturn;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.Implementations.Transactions;

public class GoodsReturnIntegration : IGoodsReturnIntegration
{
    public Task<GoodsReturnHeaderSAPDTO?> GetGoodsReturnHeaderAsync(int docEntry)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<GoodsReturnLineSAPDTO>> GetGoodsReturnLinesAsync(int docEntry)
    {
        throw new NotImplementedException();
    }

    public Task<(IEnumerable<GoodsReturnsSAPDTO>, int)> GetGoodsReturnsListAsync(DataGridIntent intent)
    {
        throw new NotImplementedException();
    }

    public Task<GRRHeaderSAPDTO?> GetGRRHeaderAsync(int docEntry)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<GRRLineSAPDTO>> GetGRRLinesAsync(int docEntry)
    {
        throw new NotImplementedException();
    }

    public Task<(IEnumerable<GoodsReturnRequestsSAPDTO>, int)> GetGRRsListAsync(DataGridIntent intent)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<ReturnTypeSAPDTO>> GetReturnTypesAsync()
    {
        throw new NotImplementedException();
    }

    public Task<bool> PostGoodsReturnAsync(GoodsReturnDTO data)
    {
        throw new NotImplementedException();
    }

    public Task<bool> PostGoodsReturnFromGRPOAsync(GoodsReturnDTO data)
    {
        throw new NotImplementedException();
    }

    public Task<bool> PostGoodsReturnFromGRRAsync(GoodsReturnDTO data)
    {
        throw new NotImplementedException();
    }
}
