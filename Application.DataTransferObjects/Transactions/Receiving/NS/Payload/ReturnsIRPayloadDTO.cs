using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Payload;
using System.Text.Json.Serialization;

namespace Application.DataTransferObjects.Transactions.Receiving.NS.Payload;
public class ReturnsIRPayloadDTO
{
    [JsonPropertyName("custbody_dbti_receiving_category")]
    public int ReceivingCategory { get; set; }

    [JsonPropertyName("custbody_dbti_received_by")]
    public int EmployeeId { get; set; }
    [JsonPropertyName("itemfulfillment")]
    public int ItemFulfillmentId { get; set; }

    [JsonPropertyName("item")]
    public ItemContainer Item { get; set; } = new();

    public static ReturnsIRPayloadDTO CreateForItemReceipt(
        List<PostReturnsDTO> lines,
        int receivingCategory,
        int ifOrderId,
        int userId
        )
    {
        // Make it nullable if its not included in json
        return new ReturnsIRPayloadDTO
        {
            ReceivingCategory = receivingCategory,
            EmployeeId = userId,
            ItemFulfillmentId = ifOrderId,
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
                                        Id = line.IsMissing ? "6" : line.IsBad ? "3" : "1",
                                    },
                                    Quantity = line.ScannedQuantity,
                                    BinNumber = !line.IsLocationUsedBin ? null :
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

                        var inventoryDetail = new InventoryDetail
                        {
                            InventoryAssignment = assignment
                        };

                        //var inventoryDetail = new InventoryDetail
                        //{
                        //    InventoryAssignmentList = new InventoryAssignmentList
                        //    {
                        //        InventoryAssignment = assignment
                        //    }
                        //};

                        return new OrderLineItem
                        {
                            OrderLine = line.LineSequenceNumber,
                            isReceived = true,
                            Quantity = line.ScannedQuantity,
                            RecordWeight = line.TotalWeight,
                            ActualWeight = line.ScannedWeight,
                            Rate = line.IsBad ? 0 : null,
                            InventoryDetail = inventoryDetail
                        };

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

                        //var inventoryDetail = line.NetsuiteMaterialPrefferedBinId != 0
                        //? new InventoryDetail
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
                        //    RecordWeight = line.TotalWeight,
                        //    ActualWeight = line.ScannedWeight,
                        //    Rate = line.IsBad ? 0 : null,
                        //    InventoryDetail = inventoryDetail
                        //};
                    }
                }).ToList()
            }
        };
    }
}
