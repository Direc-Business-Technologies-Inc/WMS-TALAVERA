using Application.DataTransferObjects.Transactions.Commons.NS;
using System.Text.Json.Serialization;

namespace Application.DataTransferObjects.Transactions.Receiving.NS.Payload;
public class ReturnsPayloadDTO
{
    [JsonPropertyName("item")]
    public ItemContainer Item { get; set; } = new();

    public static ReturnsPayloadDTO CreateForItemReceipt(
        List<PostReturnsDTO> lines)
    {
        // Make it nullable if its not included in json
        return new ReturnsPayloadDTO
        {
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
