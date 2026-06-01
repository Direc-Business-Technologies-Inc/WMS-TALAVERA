using Application.DataTransferObjects.Transactions.GoodsReturn;
using Application.DataTransferObjects.Transactions.GoodsReturn.SAP;
using Shared.Entities;

namespace Application.UseCases.Repositories.Integration.Transaction.GoodsReturn;

public interface IGoodsReturnIntegration
{
    public Task<(IEnumerable<GoodsReturnRequestsSAPDTO>, int)> GetGRRsListAsync(DataGridIntent intent);
    public Task<GRRHeaderSAPDTO?> GetGRRHeaderAsync(int docEntry);
    public Task<IEnumerable<GRRLineSAPDTO>> GetGRRLinesAsync(int docEntry);
    public Task<(IEnumerable<GoodsReturnsSAPDTO>, int)> GetGoodsReturnsListAsync(DataGridIntent intent);
    public Task<GoodsReturnHeaderSAPDTO?> GetGoodsReturnHeaderAsync(int docEntry);
    public Task<IEnumerable<GoodsReturnLineSAPDTO>> GetGoodsReturnLinesAsync(int docEntry);
    public Task<bool> PostGoodsReturnAsync(GoodsReturnDTO data);
    public Task<bool> PostGoodsReturnFromGRRAsync(GoodsReturnDTO data);
    public Task<bool> PostGoodsReturnFromGRPOAsync(GoodsReturnDTO data);
    public Task<IEnumerable<ReturnTypeSAPDTO>> GetReturnTypesAsync();
}
