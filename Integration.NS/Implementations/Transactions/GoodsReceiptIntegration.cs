using Application.DataTransferObjects.Transactions.GoodsReceipt;
using Application.UseCases.Repositories.Integration.Transaction.GoodsReceipt;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.Implementations.Transactions;

public class GoodsReceiptIntegration : IGoodsReceiptIntegration
{
    public Task<(IEnumerable<GoodsReceiptDataGridSAPDTO> Data, int Count)> GetApprovedGoodsReceiptDataGrid(DataGridIntent intent)
    {
        throw new NotImplementedException();
    }

    public Task<(IEnumerable<GoodsReceiptDataGridSAPDTO> Data, int Count)> GetGoodsReceiptDraftDataGrid(DataGridIntent intent, string status)
    {
        throw new NotImplementedException();
    }

    public Task<GoodsReceiptHeaderSAPDTO?> GetGoodsReceiptDraftHeader(int docEntry)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<GoodsReceiptLineSAPDTO>> GetGoodsReceiptDraftLines(int docEntry)
    {
        throw new NotImplementedException();
    }

    public Task<GoodsReceiptHeaderSAPDTO?> GetGoodsReceiptHeader(int docEntry)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<GoodsReceiptLineSAPDTO>> GetGoodsReceiptLines(int docEntry)
    {
        throw new NotImplementedException();
    }

    public Task<bool> PostGoodsReceipt(GoodsReceiptDTO data)
    {
        throw new NotImplementedException();
    }
}
