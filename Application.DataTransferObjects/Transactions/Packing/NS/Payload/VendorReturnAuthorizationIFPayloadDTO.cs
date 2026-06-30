using Application.DataTransferObjects.Transactions.Commons.NS.Payload;
using System.Text.Json.Serialization;

namespace Application.DataTransferObjects.Transactions.Packing.NS.Payload;

public class VendorReturnAuthorizationIFPayloadDTO
{
    [JsonPropertyName("shipStatus")]
    public string ShipStatus { get; set; }
    [JsonPropertyName("item")]
    public ItemContainer Item { get; set; } = new();

    public static VendorReturnAuthorizationIFPayloadDTO CreateForItemFulfillment(
        List<PostVendorReturnAuthorizationDTO> lines,
        string shipStatus)
    {
        // Make it nullable if its not included in json
        return new VendorReturnAuthorizationIFPayloadDTO
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
                            InventoryDetail = new InventoryDetail
                            {
                                InventoryAssignmentList = new InventoryAssignmentList
                                {
                                    InventoryAssignment = new InventoryAssignment
                                    {
                                        Items = new List<InventoryAssignmentItem>
                                        {
                                            new()
                                            {
                                                InventoryStatus = new ReferenceValue
                                                {
                                                    Id = line.IsBad ? "3" : "1"
                                                },
                                                BinNumber = line.IsLocationUsedBin ? new ReferenceValue
                                                {
                                                    Id = line.VendorBinAssignmentId != 0
                                                                ? line.VendorBinAssignmentId.ToString()
                                                                : line.NetsuiteMaterialPrefferedBinId.ToString()
                                                } : null,
                                                Quantity = line.ScannedQuantity
                                            }
                                        }
                                    }
                                }
                            }
                        };
                    }
                }).ToList()
            }
        };
    }
}
