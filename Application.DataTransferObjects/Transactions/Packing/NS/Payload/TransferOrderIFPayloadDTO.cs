using System.Text.Json.Serialization;
using static Application.DataTransferObjects.Transactions.InventoryCounting.NS.Payload.PostInventoryWorksheetPayload;

namespace Application.DataTransferObjects.Transactions.Commons.NS.Payload;

public class TransferOrderIFPayloadDTO
{
    [JsonPropertyName("shipStatus")]
    public string ShipStatus { get; set; }
    [JsonPropertyName("item")]
    public ItemContainer Item { get; set; } = new();

    public static TransferOrderIFPayloadDTO CreateForItemFulfillment(
        List<PostTransferOrderDTO> lines,
        string shipStatus)
    {
        // Make it nullable if its not included in json
        return new TransferOrderIFPayloadDTO
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
                        var assignment = new InventoryAssignment
                        {
                            Items =
                            [
                                new InventoryAssignmentItem
                                {
                                    InventoryStatus = new ReferenceValue
                                    {
                                        Id = /*line.IsBad ? "3" :*/ "1"
                                    },
                                    Quantity = line.ScannedQuantity,
                                    BinNumber = line.NetsuiteMaterialVendorAssignedBin != 0
                                        ? new ReferenceValue
                                        {
                                            Id = line.NetsuiteMaterialVendorAssignedBin.ToString()
                                        }
                                        : line.NetsuiteMaterialPrefferedBinId != 0
                                        ? new ReferenceValue
                                        {
                                            Id = line.NetsuiteMaterialPrefferedBinId.ToString()
                                        }
                                        : null
                                }
                            ]
                        };

                        var inventoryDetail = line.NetsuiteMaterialPrefferedBinId != 0
                        ? new InventoryDetail
                        {
                            InventoryAssignmentList = new InventoryAssignmentList
                            {
                                InventoryAssignment = assignment
                            }
                        }
                        : new InventoryDetail
                        {
                            InventoryAssignment = assignment
                        };

                        return new OrderLineItem
                        {
                            OrderLine = line.LineSequenceNumber,
                            isReceived = true,
                            Quantity = line.ScannedQuantity,
                            InventoryDetail = inventoryDetail
                        };
                    }
                }).ToList()
            }
        };
    }
}