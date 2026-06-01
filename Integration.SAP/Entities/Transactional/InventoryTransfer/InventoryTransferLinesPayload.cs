using Ardalis.GuardClauses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.SAP.Entities.Transactional.InventoryTransfer;
public class InventoryTransferLinesPayload
{
    public string ItemCode { get; private set; }
    public Decimal Quantity { get; private set; }
    public int? BaseLine { get; private set; }
    public int? BaseEntry { get; private set; }
    public int? BaseType { get; private set; }

    const int INVENTORY_TRANSFER_REQUEST_CODE = 1250000001;

    protected InventoryTransferLinesPayload(string itemCode, Decimal quantity, int? baseEntry, int? baseLine)
    {
        ItemCode = Guard.Against.NullOrEmpty(itemCode, nameof(itemCode), "Item Code cant be null");
        Quantity = Guard.Against.NegativeOrZero(quantity, nameof(quantity), "Quantity cant be zero or negative");
        BaseEntry = baseEntry;
        BaseLine = BaseEntry is null ? null : baseLine;
        BaseType = BaseEntry is null ? null : INVENTORY_TRANSFER_REQUEST_CODE;
    }

    public static InventoryTransferLinesPayload Create(string itemCode, Decimal quantity, int? baseEntry, int? baseLine)
    {
        return new InventoryTransferLinesPayload(itemCode, quantity, baseEntry, baseLine);
    }
}