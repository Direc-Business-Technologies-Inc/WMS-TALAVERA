using Application.DataTransferObjects.Transactions.Packing.Returns;
using Shared.Entities;

namespace Application.UseCases.Repositories.Integration.Transaction.Packing;

public interface IReturnPackingIntegration
{
    Task<(IEnumerable<ReturnsDataGridDTO> Data, int Count)> GetPackingReturnsList(DataGridIntent intent);
    Task<ReturnsInfoDTO?> GetPackingReturn(string id);
    Task<(IEnumerable<ReturnsLineDTO> Data, int Count)> GetPackingReturnLines(string id, DataGridIntent intent);
}
