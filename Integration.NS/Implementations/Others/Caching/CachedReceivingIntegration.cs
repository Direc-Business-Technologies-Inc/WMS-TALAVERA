using Application.DataTransferObjects.Transactions.Receiving;
using Application.UseCases.Repositories.Integration.Transaction.Receiving;
using Integration.SAP.Entities.Transactional.Receiving;
using Shared.Entities;

namespace Integration.NS.Implementations.Others.Caching;

/// <summary>
/// 60-second grid cache for receiving (Req 2.1). Only the paginated grid reads
/// are cached; header/line reads pass straight through, and posting an item
/// receipt evicts the entity's cached grid pages so the next grid load
/// refetches (Req 2.2).
/// </summary>
public class CachedReceivingIntegration(
    Transactions.ReceivingIntegration inner,
    GridResultCache cache) : IReceivingIntegration
{
    public const string EntityName = "Receiving";

    public Task<(IEnumerable<PurchaseOrderDataGridDTO>, int)> GetPurchaseOrdersListAsync(DataGridIntent intent)
        => ReadAsync(nameof(GetPurchaseOrdersListAsync), intent, inner.GetPurchaseOrdersListAsync, intent.CancellationToken);

    public Task<(IEnumerable<TransferOrderDataGridDTO>, int count)> GetTransferOrderListAsync(DataGridIntent intent)
        => ReadAsync(nameof(GetTransferOrderListAsync), intent, inner.GetTransferOrderListAsync, intent.CancellationToken);

    public Task<(IEnumerable<ReturnsDataGridDTO>, int count)> GetReturnsListAsync(DataGridIntent intent)
        => ReadAsync(nameof(GetReturnsListAsync), intent, inner.GetReturnsListAsync, intent.CancellationToken);

    public Task<(IEnumerable<ItemReceiptDataGridDTO> data, int count)> GetItemReceiptsDatagrid(DataGridIntent intent)
        => ReadAsync(nameof(GetItemReceiptsDatagrid), intent, inner.GetItemReceiptsDatagrid, intent.CancellationToken);

    public Task<Application.DataTransferObjects.Transactions.PrintStatusCountsDTO> GetReceivingPrintStatusCounts(DataGridIntent intent, CancellationToken ct = default)
        => ReadAsync(nameof(GetReceivingPrintStatusCounts), intent,
            snapshot => inner.GetReceivingPrintStatusCounts(snapshot, snapshot.CancellationToken), ct);

    private Task<T> ReadAsync<T>(string method, DataGridIntent intent, Func<DataGridIntent, Task<T>> factory, CancellationToken ct)
    {
        var snapshot = intent.Snapshot();
        return cache.ReadAsync(EntityName, method, snapshot,
            sharedCt => factory(ForRead(snapshot, sharedCt)), ct);
    }

    private static DataGridIntent ForRead(DataGridIntent intent, CancellationToken ct)
    {
        var snapshot = intent.Snapshot();
        snapshot.CancellationToken = ct;
        return snapshot;
    }

    public Task<ItemFulfillmentPickerResult> GetSTRItemFulfillments(int strId, DataGridIntent intent)
        => cache.GetOrAddAsync(EntityName, nameof(GetSTRItemFulfillments), intent,
            () => inner.GetSTRItemFulfillments(strId, intent), default, strId);

    public Task<(IEnumerable<Application.DataTransferObjects.Transactions.Receiving.NS.ReceivingLineNSDTO>, int)> GetTransferOrderLinesAsync(string id, DataGridIntent intent)
        => cache.GetOrAddAsync(EntityName, nameof(GetTransferOrderLinesAsync), intent,
            () => inner.GetTransferOrderLinesAsync(id, intent), default, id);

    public Task<(IEnumerable<PurchaseDeliveryNoteSAPDTO>, int)> GetPurchaseDeliveryNotesListAsync(DataGridIntent intent)
        => cache.GetOrAddAsync(EntityName, nameof(GetPurchaseDeliveryNotesListAsync), intent,
            () => inner.GetPurchaseDeliveryNotesListAsync(intent));

    public Task<(IEnumerable<PurchaseOrderStatusDTO>, int)> GetPurchaseOrderStatuses(DataGridIntent intent)
        => inner.GetPurchaseOrderStatuses(intent);

    public Task<(IEnumerable<TransferOrderStatusDTO>, int)> GetTransferOrderStatuses(DataGridIntent intent)
        => inner.GetTransferOrderStatuses(intent);

    public Task<PurchaseOrderDTO?> GetPurchaseOrderHeaderAsync(string docEntry)
        => inner.GetPurchaseOrderHeaderAsync(docEntry);

    public Task<TransferOrderDTO?> GetTransferOrderHeaderAsync(string docEntry)
        => inner.GetTransferOrderHeaderAsync(docEntry);

    public Task<ReturnsDTO?> GetReturnsHeaderAsync(string docEntry)
        => inner.GetReturnsHeaderAsync(docEntry);

    public Task<ItemReceiptDTO?> GetItemReceiptHeaderAsync(string docEntry)
        => inner.GetItemReceiptHeaderAsync(docEntry);

    public Task<IEnumerable<PurchaseOrderLineDTO>> GetPurchaseOrderLinesAsync(string docEntry)
        => inner.GetPurchaseOrderLinesAsync(docEntry);

    public Task<IEnumerable<ReturnsLineDTO>> GetReturnsLinesAsync(string docEntry)
        => inner.GetReturnsLinesAsync(docEntry);

    public Task<IEnumerable<ItemReceiptLineDTO>> GetItemReceiptLinesAsync(string docEntry, bool isTransferOrder = false)
        => inner.GetItemReceiptLinesAsync(docEntry, isTransferOrder);

    public Task<IEnumerable<ItemFulfillmentLineDTO>> GetItemFulfillmentLines(int ifId, DataGridIntent intent)
        => inner.GetItemFulfillmentLines(ifId, intent);

    public Task<IEnumerable<ItemReceiptLineDTO>> GetItemReceiptItemFulfillmentLinesAsync(string docEntry)
        => inner.GetItemReceiptItemFulfillmentLinesAsync(docEntry);

    public Task<BarcodeDTO?> GetBarcodeData(string barcode, int? location)
        => inner.GetBarcodeData(barcode, location);

    public async Task<bool> PostItemReceipt(ItemReceiptDTO itemReceiptDTO)
    {
        var posted = await inner.PostItemReceipt(itemReceiptDTO);
        if (posted)
        {
            cache.Invalidate(EntityName);
        }

        return posted;
    }
}
