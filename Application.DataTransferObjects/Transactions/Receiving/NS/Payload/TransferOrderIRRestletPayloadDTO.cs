using Application.DataTransferObjects.Transactions.Commons.NS;
using System.Text.Json.Serialization;

namespace Application.DataTransferObjects.Transactions.Receiving.NS.Payload;

public class TransferOrderIRRestletPayloadDTO
{
    [JsonPropertyName("transferOrderId")]
    public int TransferOrderId { get; set; }
    [JsonPropertyName("transferCategory")]
    public int TransferCategory { get; set; }
    [JsonPropertyName("receiverEmployeeId")]
    public int ReceiverEmployeeId { get; set; }

    [JsonPropertyName("custbody_dbti_prepared_by")]
    public int PreparedEmployeeId { get; set; }

    [JsonPropertyName("fulfillmentId")]
    public int ItemFulfillmentId { get; set; }

    [JsonPropertyName("lines")]
    public List<LinesContainer> Lines { get; set; } = new();

    public static TransferOrderIRRestletPayloadDTO CreateForItemReceiptRestlet(
    List<PostTransferOrderDTO> lines,
    int transferOrderId,
    int ifOrderId,
    int userId,
    int receivingCategory)
    {
        return new TransferOrderIRRestletPayloadDTO
        {
            TransferOrderId = transferOrderId,
            ReceiverEmployeeId = userId,
            PreparedEmployeeId = userId,
            TransferCategory = receivingCategory,
            ItemFulfillmentId = ifOrderId,
            Lines = lines.Select(line => new LinesContainer
            {
                OrderLine = line.LineSequenceNumber,
                Quantity = line.ScannedQuantity,
                Rate = line.IsBad ? 0 : null,
                RecordWeight = line.TotalWeight,
                ActualWeight = line.ScannedWeight,

                InventoryDetail = new List<LinesInventoryDetail>
                {
                    new LinesInventoryDetail
                    {
                        InventoryStatus = line.IsBad ? 3 : 1,
                        Quantity = line.ScannedQuantity
                    }
                }
            }).ToList()
        };
    }

    public class LinesContainer
    {
        [JsonPropertyName("orderLine")]
        public int OrderLine { get; set; }

        [JsonPropertyName("quantity")]
        public decimal? Quantity { get; set; }

        [JsonPropertyName("recWeight")]
        public decimal? RecordWeight { get; set; }

        [JsonPropertyName("actualWeight")]
        public decimal? ActualWeight { get; set; }

        [JsonPropertyName("rate")]
        public decimal? Rate { get; set; }

        [JsonPropertyName("inventoryDetail")]
        public List<LinesInventoryDetail> InventoryDetail { get; set; } = new();
    }

    public class LinesInventoryDetail
    {
        [JsonPropertyName("inventoryStatus")]
        public int InventoryStatus { get; set; } = new();

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }
    }
}