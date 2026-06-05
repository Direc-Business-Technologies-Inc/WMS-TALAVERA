using Shared.Libraries.ViewModel;
using System.Text.Json.Serialization;

namespace Application.DataTransferObjects.Transactions.Receiving.NS.Payload;

public class TransferOrderPayloadDTO
{
    [JsonPropertyName("custbody_dbti_receiving_category")]
    public int ReceivingCategory { get; set; }
    [JsonPropertyName("item")]
    public ItemContainer Item { get; set; } = new();

    public static TransferOrderPayloadDTO CreateForItemReceipt(
        List<TransferOrderLineVM> lines,
        int receivingCategory)
    {
        // Make it nullable if its not included in json
        return new TransferOrderPayloadDTO
        {
            ReceivingCategory = receivingCategory, // 1 is Good , 2 is Bad
            Item = new ItemContainer
            {
                Items = lines.Select(line =>
                {
                    if (line.ScannedQuantity == 0)
                    {
                        return new OrderLineItem
                        {
                            OrderLine = line.LineSequenceNumber,
                            isReceived = false
                        };
                    }

                    return new OrderLineItem
                    {
                        OrderLine = line.LineSequenceNumber,
                        isReceived = true,
                        Quantity = line.ScannedQuantity,
                        RecordWeight = line.TotalWeight,
                        ActualWeight = line.ScannedWeight,
                        Rate = line.IsBad ? 0 : null,
                    };
                }).ToList()
            }
        };
    }
}