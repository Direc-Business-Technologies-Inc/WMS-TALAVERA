using Shared.Entities;
using Web.BlazorServer.ViewModels.Transaction.Packing;
using Web.BlazorServer.ViewModels.Transaction.Packing.Returns;

namespace Web.BlazorServer.Handlers.Repositories.Transaction.Packing.Returns;

public interface IReturnPackingHandler
{
    Task<(IEnumerable<ReturnsPackingDataGridVM> Data, int Count)> GetReturnsList(DataGridIntent intent, int subsidiaryId, bool includeReceived = false);
    Task<ReturnsInfoPackingVM?> GetPackingReturn(string reference);
    Task<(IEnumerable<ReturnsLinePackingVM> Data, int Count)> GetPackingReturnLines(string reference, DataGridIntent intent);
    Task<(IEnumerable<PackedItemFulfillmentVM> Data, int Count)> GetPackedItemFulfillments(DataGridIntent intent);
    Task<Application.DataTransferObjects.Transactions.PrintStatusCountsDTO> GetPackingPrintStatusCounts(DataGridIntent intent, CancellationToken ct = default);

}
