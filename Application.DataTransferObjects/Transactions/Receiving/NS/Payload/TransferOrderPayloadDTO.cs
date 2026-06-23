using System.Text.Json.Serialization;

namespace Application.DataTransferObjects.Transactions.Receiving.NS.Payload;

public class TransferOrderPayloadDTO
{
    //[JsonPropertyName("custbody_dbti_receiving_category")]
    //public int ReceivingCategory { get; set; }
    [JsonPropertyName("transferOrderId")]
    public int TransferOrderId { get; set; }
    [JsonPropertyName("transferCategory")]
    public int TransferCategory { get; set; }
    [JsonPropertyName("lines")]
    public List<LinesContainer> Lines { get; set; } = new();

    //public static TransferOrderPayloadDTO CreateForItemReceipt(
    //    List<PostTransferOrderDTO> lines,
    //    int receivingCategory)
    //{
    //    // Make it nullable if its not included in json
    //    return new TransferOrderPayloadDTO
    //    {
    //        ReceivingCategory = receivingCategory, // 1 is Good , 2 is Bad
    //        Item = new ItemContainer
    //        {
    //            Items = lines.Select(line =>
    //            {
    //                if (line.ScannedQuantity == 0)
    //                {
    //                    return new OrderLineItem
    //                    {
    //                        OrderLine = line.LineSequenceNumber,
    //                        isReceived = false
    //                    };
    //                }

    //                return new OrderLineItem
    //                {
    //                    OrderLine = line.LineSequenceNumber,
    //                    isReceived = true,
    //                    Quantity = line.ScannedQuantity,
    //                    RecordWeight = line.TotalWeight,
    //                    ActualWeight = line.ScannedWeight,
    //                    Rate = line.IsBad ? 0 : null,
    //                };
    //            }).ToList()
    //        }
    //    };
    //}

    public static TransferOrderPayloadDTO CreateForItemReceiptRestlet(
    List<PostTransferOrderDTO> lines,
    int transferOrderId,
    int receivingCategory)
    {
        return new TransferOrderPayloadDTO
        {
            TransferOrderId = transferOrderId,
            TransferCategory = receivingCategory,

            Lines = lines.Select(line => new LinesContainer
            {
                OrderLine = line.LineSequenceNumber,
                Quantity = line.ScannedQuantity,
                //RecordWeight = line.TotalWeight,
                //ActualWeight = line.ScannedWeight,
                Rate = line.IsBad ? 0 : (decimal?)null,

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

        [JsonPropertyName("rate")]
        public decimal? Rate { get; set; }

        //[JsonPropertyName("custcol_dbti_record_weight")]
        //public decimal RecordWeight { get; set; }

        //[JsonPropertyName("custcol_dbti_actual_weight")]
        //public decimal ActualWeight { get; set; }

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