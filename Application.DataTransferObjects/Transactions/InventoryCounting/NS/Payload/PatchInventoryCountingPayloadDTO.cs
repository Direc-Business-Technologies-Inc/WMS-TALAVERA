using Application.DataTransferObjects.Transactions.Commons.NS.Payload;
using System.Text.Json.Serialization;

namespace Application.DataTransferObjects.Transactions.InventoryCounting.NS.Payload;

public class PatchInventoryCountingPayloadDTO
{
    [JsonPropertyName("item")]
    public CountItemContainer Item { get; set; } = new();

    public static PatchInventoryCountingPayloadDTO PatchInventoryCounting(
    List<PatchInventoryCountingDTO> lines)
    {
        return new PatchInventoryCountingPayloadDTO
        {
            Item = new CountItemContainer
            {
                Items = lines.Select(line => new CountLineItem
                {
                    CountLine = line.LineSequenceNumber,
                    CountQuantity = line.ScannedQuantity,
                    CountDetail = new CountDetail
                    {
                        CountInventoryDetail = new CountInventoryDetail
                        {
                            Items =
                            [
                                new CountInventoryDetailItem
                                {
                                    Id = line.NetsuiteInventoryDetailInternalId,
                                    Quantity = line.ScannedQuantity,
                                    InventoryStatus = new ReferenceValue
                                    {
                                        Id = "1"
                                    }
                                }
                            ]
                        }
                    }
                }).ToList()
            }
        }; ;
    }
}
