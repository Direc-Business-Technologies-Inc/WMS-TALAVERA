using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Payload;
using System.Text.Json.Serialization;

namespace Application.DataTransferObjects.Transactions.Packing.NS.Payload;
public class ReturnsIFPayloadDTO
{
    [JsonPropertyName("shipStatus")]
    public string ShipStatus { get; set; }
    [JsonPropertyName("custbody_dbti_print_label")]
    public bool PrintLabel { get; set; }
    [JsonPropertyName("item")]
    public ItemContainer Item { get; set; } = new();

    public static ReturnsIFPayloadDTO CreateForItemFulfillment(
        List<PostReturnsDTO> lines,
        string shipStatus,
        bool printLabel,
        bool isUsedBin)
    {
        // Make it nullable if its not included in json
        return new ReturnsIFPayloadDTO
        {
            ShipStatus = shipStatus,
            PrintLabel = printLabel,
            Item = new ItemContainer
            {
                Items = lines.Select(line =>
                {
                    if (line.ScannedQuantity == 0)
                    {
                        // On bin-tracked locations the transform URL carries ?replace=item.inventoryDetail.inventoryAssignment,
                        // and NetSuite rejects the request unless the body's item entries carry the sub-resource consistently.
                        // Zero-qty filler lines therefore get an EMPTY assignment so the replace target exists on every line.
                        var filler = new OrderLineItem
                        {
                            OrderLine = line.LineSequenceNumber,
                            isReceived = false
                        };

                        if (isUsedBin)
                        {
                            filler.InventoryDetail = new InventoryDetail
                            {
                                InventoryAssignment = new InventoryAssignment
                                {
                                    Items = []
                                }
                            };
                        }

                        return filler;
                    }
                    else
                    {
                        // Legacy working shape (pre-2026-07): non-bin locations send no inventoryDetail and let NetSuite auto-manage it.
                        // Bin-tracked locations need the explicit REST-shaped detail plus ?replace= on the transform URL.
                        if (!isUsedBin)
                        {
                            return new OrderLineItem
                            {
                                OrderLine = line.LineSequenceNumber,
                                isReceived = true,
                                Quantity = line.ScannedQuantity
                            };
                        }

                        var assignment = new InventoryAssignment
                        {
                            Items =
                            [
                                new InventoryAssignmentItem
                                {
                                    InventoryStatus = new ReferenceValue
                                    {
                                        Id = line.TransferCategory == 3 ? "1" : "3" //if transfer category is good then status is good
                                    },
                                    Quantity = line.ScannedQuantity,
                                    // Use the header isUsedBin (same flag that decides ?replace= and filler
                                    // shape) so a stale per-line flag can never null the bin on one line
                                    // while the transform still demands inventoryDetail.
                                    BinNumber = !isUsedBin ? null :
                                        line.NetsuiteMaterialVendorAssignedBin != 0
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

                        //var inventoryDetail = !isUsedBin ? new InventoryDetail
                        //{
                        //    InventoryAssignmentList = new InventoryAssignmentList
                        //    {
                        //        InventoryAssignment = assignment
                        //    }
                        //}
                        //: new InventoryDetail
                        //{
                        //    InventoryAssignment = assignment
                        //};

                        //return new OrderLineItem
                        //{
                        //    OrderLine = line.LineSequenceNumber,
                        //    isReceived = true,
                        //    Quantity = line.ScannedQuantity,
                        //    InventoryDetail = inventoryDetail
                        //};

                        //var assignment = new InventoryAssignment
                        //{
                        //    Items =
                        //    [
                        //        new InventoryAssignmentItem
                        //        {
                        //            InventoryStatus = new ReferenceValue
                        //            {
                        //                Id = /*line.IsBad ? "3" :*/ "1"
                        //            },
                        //            Quantity = line.ScannedQuantity,
                        //            BinNumber = !line.IsLocationUsedBin ? null :
                        //                line.NetsuiteMaterialVendorAssignedBin != 0
                        //                ? new ReferenceValue
                        //                {
                        //                    Id = line.NetsuiteMaterialVendorAssignedBin.ToString()
                        //                }
                        //                : line.NetsuiteMaterialPrefferedBinId != 0
                        //                ? new ReferenceValue
                        //                {
                        //                    Id = line.NetsuiteMaterialPrefferedBinId.ToString()
                        //                }
                        //                : null
                        //        }
                        //    ]
                        //};

                        var inventoryDetail = new InventoryDetail
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
