using Application.DataTransferObjects.Transactions.GoodsReceipt;
using Shared.Entities;

namespace Application.UseCases.Repositories.Integration.Transaction.GoodsReceipt;

public interface IGoodsReceiptIntegration
{
    Task<(IEnumerable<GoodsReceiptDataGridSAPDTO> Data, int Count)> GetApprovedGoodsReceiptDataGrid(DataGridIntent intent);
    Task<(IEnumerable<GoodsReceiptDataGridSAPDTO> Data, int Count)> GetGoodsReceiptDraftDataGrid(DataGridIntent intent, string status);
    Task<GoodsReceiptHeaderSAPDTO?> GetGoodsReceiptHeader(int docEntry);
    Task<IEnumerable<GoodsReceiptLineSAPDTO>> GetGoodsReceiptLines(int docEntry);
    Task<GoodsReceiptHeaderSAPDTO?> GetGoodsReceiptDraftHeader(int docEntry);
    Task<IEnumerable<GoodsReceiptLineSAPDTO>> GetGoodsReceiptDraftLines(int docEntry);
    Task<bool> PostGoodsReceipt(GoodsReceiptDTO data);
}
