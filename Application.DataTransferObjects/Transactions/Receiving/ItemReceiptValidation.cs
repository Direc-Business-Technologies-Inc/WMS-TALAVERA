namespace Application.DataTransferObjects.Transactions.Receiving;

/// <summary>Preflight contract for the web item-receipt workflow, before any NetSuite writes.</summary>
public static class ItemReceiptValidation
{
    public static void Validate(ItemReceiptDTO receipt)
    {
        if (receipt.SourceInternalId <= 0)
            throw new InvalidOperationException("Please select a valid source order for item receipt.");

        if (receipt.SourceType != ItemReceiptDTO.SourceTypes.PurchaseOrder &&
            receipt.ItemFulfillmentId.GetValueOrDefault() <= 0)
            throw new InvalidOperationException("Please select a shipped Item Fulfillment before receiving this transfer order.");

        var selected = receipt.Lines.Where(line => line.IsReceived).ToList();
        foreach (var line in selected)
        {
            var label = $"Line '{line.ItemCode}' ({line.ItemDescription})";
            if (line.QuantityAlloted < 0 || line.InventoryDetails.Any(detail => detail.QuantityAlloted < 0))
                throw new InvalidOperationException($"{label}: receipt quantities cannot be negative.");

            if (line.QuantityAlloted == 0 && line.InventoryDetails.All(detail => detail.QuantityAlloted == 0))
                continue;

            if (line.LineNumber <= 0)
                throw new InvalidOperationException($"{label} has no NetSuite line number. Reload the source document and retry.");

            if (line.QuantityAlloted > line.QuantityOpen)
                throw new InvalidOperationException($"{label}: allotted {line.QuantityAlloted} exceeds remaining {line.QuantityOpen}. Reload and adjust quantities.");

            if (line.InventoryDetails.Sum(detail => detail.QuantityAlloted) != line.QuantityAlloted)
                throw new InvalidOperationException($"{label}: assign inventory details matching the receipt quantity ({line.QuantityAlloted}), including for non-bin items.");

            if (line.InventoryDetails.Where(detail => detail.QuantityAlloted > 0)
                .Any(detail => detail.Status?.Id is not (1 or 3 or 6)))
                throw new InvalidOperationException($"{label}: every positive allocation must have a GOOD, BAD/CONFISCATED, or MISSING inventory status.");

            if (receipt.SourceType == ItemReceiptDTO.SourceTypes.PurchaseOrder &&
                line.IsLocationBinUsed && line.ItemUsesBins &&
                line.InventoryDetails.Any(detail => detail.QuantityAlloted > 0 && detail.Bin?.Id is not > 0))
                throw new InvalidOperationException($"{label}: select a valid bin for every positive inventory allocation.");
        }

        if (!selected.Any(line => line.QuantityAlloted > 0))
            throw new InvalidOperationException("Select at least one line with a positive receipt quantity and valid inventory details.");

        if (selected.Where(line => line.QuantityAlloted > 0).GroupBy(line => line.LineNumber).Any(group => group.Count() > 1))
            throw new InvalidOperationException("The receipt contains duplicate source lines. Reload the source document and retry.");
    }
}
