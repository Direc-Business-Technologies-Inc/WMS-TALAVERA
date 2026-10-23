using System.Text.Json.Serialization;
using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Payload;

namespace Application.DataTransferObjects.Transactions.Packing.NS.Payload;

/// <summary>
/// Payload for the transfer-order item fulfillment RESTlet (NetSuite script 2522).
/// Unlike the REST <c>!transform/itemFulfillment</c> contract this carries ONLY the
/// lines the operator scanned: there are no zero-quantity filler lines, no
/// <c>?replace=item.inventoryDetail.inventoryAssignment</c> parameter, and therefore
/// none of the static-sublist constraints that rejected the 2026-10-23 field saves
/// (order STR-THM00100000881 / 226006).
/// </summary>
/// <remarks>
/// The script is responsible for not fulfilling the lines absent from <see cref="Lines"/>.
/// That is a property of the deployed SuiteScript, not of this payload - verify it on a
/// partially scanned transfer order before enabling the path in production.
/// </remarks>
public class TransferOrderIFRestletPayloadDTO
{
    [JsonPropertyName("transferOrderId")]
    public int TransferOrderId { get; set; }

    [JsonPropertyName("shipStatus")]
    public string ShipStatus { get; set; } = "B";

    [JsonPropertyName("lines")]
    public List<RestletLine> Lines { get; set; } = [];

    /// <summary>
    /// Builds the payload from the reconciled rows. Rows are grouped by order line so a
    /// line split across qualities (or bins) becomes one entry carrying several
    /// assignments; <c>quantity</c> is their sum. At a non-bin-tracked location the
    /// assignments are omitted entirely and NetSuite manages the detail.
    /// </summary>
    public static TransferOrderIFRestletPayloadDTO CreateForItemFulfillmentRestlet(
        List<PostTransferOrderDTO> scannedLines,
        int transferOrderId,
        string shipStatus,
        bool isUsedBin)
    {
        return new TransferOrderIFRestletPayloadDTO
        {
            TransferOrderId = transferOrderId,
            ShipStatus = shipStatus,
            Lines = scannedLines
                .Where(x => x.ScannedQuantity > 0)
                .GroupBy(x => x.LineSequenceNumber)
                .OrderBy(g => g.Key)
                .Select(g => new RestletLine
                {
                    OrderLine = g.Key,
                    Quantity = g.Sum(x => x.ScannedQuantity),
                    InventoryAssignments = !isUsedBin
                        ? null
                        : [.. g.Select(x => new RestletInventoryAssignment
                        {
                            BinNumber = x.NetsuiteMaterialVendorAssignedBin != 0
                                ? x.NetsuiteMaterialVendorAssignedBin
                                : x.NetsuiteMaterialPrefferedBinId != 0
                                    ? x.NetsuiteMaterialPrefferedBinId
                                    : null,
                            InventoryStatus = int.TryParse(
                                InventoryStatusMap.Resolve(x.IsBad, x.IsMissing, x.TransferCategory),
                                out var status)
                                ? status
                                : null,
                            Quantity = x.ScannedQuantity
                        })]
                })
                .ToList()
        };
    }

    public class RestletLine
    {
        [JsonPropertyName("orderLine")]
        public int OrderLine { get; set; }

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("inventoryAssignments")]
        public List<RestletInventoryAssignment>? InventoryAssignments { get; set; }
    }

    public class RestletInventoryAssignment
    {
        [JsonPropertyName("binNumber")]
        public int? BinNumber { get; set; }

        [JsonPropertyName("inventoryStatus")]
        public int? InventoryStatus { get; set; }

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }
    }
}

/// <summary>
/// What RESTlet script 2522 returns after committing the fulfillment:
/// <c>{ "success": true, "itemFulfillmentId": 123, "status": "Packed" }</c>.
/// Captured so a save can be verified without opening NetSuite: the log names the
/// created document, and <c>success: false</c> fails the save instead of being ignored.
/// </summary>
public class TransferOrderIFRestletResponseDTO
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("itemFulfillmentId")]
    public int ItemFulfillmentId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
