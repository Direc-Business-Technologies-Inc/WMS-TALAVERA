using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Payload;
using System.Text.Json.Serialization;

namespace Application.DataTransferObjects.Transactions.Receiving.NS.Payload;
public class ReturnsIRPayloadDTO
{
    [JsonPropertyName("custbody_dbti_receiving_category")]
    public int ReceivingCategory { get; set; }
    [JsonPropertyName("item")]
    public ItemContainer Item { get; set; } = new();

    public static ReturnsIRPayloadDTO CreateForItemReceipt(
        List<PostReturnsDTO> lines,
        int receivingCategory)
    {
        // Make it nullable if its not included in json
        return new ReturnsIRPayloadDTO
        {
            //ReceivingCategory = receivingCategory,
            //Item = new ItemContainer
            //{
            //    Items = lines.Select(line =>
            //    {
            //        if (line.ScannedQuantity == 0)
            //        {
            //            return new OrderLineItem
            //            {
            //                OrderLine = line.LineSequenceNumber,
            //                isReceived = false
            //            };
            //        }
            //        else
            //        {
            //            return new OrderLineItem
            //            {
            //                OrderLine = line.LineSequenceNumber,
            //                isReceived = true,
            //                Quantity = line.ScannedQuantity,
            //                RecordWeight = line.TotalWeight,
            //                ActualWeight = line.ScannedWeight,
            //                Rate = line.IsBad ? 0 : null,
            //            };
            //        }
            //    }).ToList()
            //}
        };
    }
}
