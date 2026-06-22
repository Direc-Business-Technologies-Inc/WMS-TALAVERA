using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Payload;
using System.Text.Json.Serialization;

namespace Application.DataTransferObjects.Transactions.Packing.NS.Payload;
public class ReturnsIFPayloadDTO
{
    [JsonPropertyName("shipStatus")]
    public string ShipStatus { get; set; }
    [JsonPropertyName("item")]
    public ItemContainer Item { get; set; } = new();

    public static ReturnsIFPayloadDTO CreateForItemFulfillment(
        List<PostReturnsDTO> lines,
        string shipStatus)
    {
        // Make it nullable if its not included in json
        return new ReturnsIFPayloadDTO
        {
            ShipStatus = shipStatus,
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
                    else
                    {
                        return new OrderLineItem
                        {
                            OrderLine = line.LineSequenceNumber,
                            isReceived = true,
                            Quantity = line.ScannedQuantity,
                        };
                    }
                }).ToList()
            }
        };
    }
}
