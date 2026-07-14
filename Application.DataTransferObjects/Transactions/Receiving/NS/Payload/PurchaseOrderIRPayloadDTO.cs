using Application.DataTransferObjects.Transactions.Commons.NS.Payload;
using System.Text.Json.Serialization;

namespace Application.DataTransferObjects.Transactions.Receiving.NS.Payload;

public class PurchaseOrderIRPayloadDTO
{
    [JsonPropertyName("custbody_dbti_receiving_category")]
    public int ReceivingCategory { get; set; }
    [JsonPropertyName("item")]
    public ItemContainer Item { get; set; } = new();

    public static PurchaseOrderIRPayloadDTO CreateForItemReceipt(
        List<PostPurchaseOrderDTO> lines,
        int receivingCategory)
    {
        // Make it nullable if its not included in json
        return new PurchaseOrderIRPayloadDTO
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
                    else
                    {
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
                        //                line.VendorBinAssignmentId != 0
                        //                ? new ReferenceValue
                        //                {
                        //                    Id = line.VendorBinAssignmentId.ToString()
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

                        //var inventoryDetail = new InventoryDetail
                        //{
                        //    InventoryAssignmentList = new InventoryAssignmentList
                        //    {
                        //        InventoryAssignment = assignment
                        //    }
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

                        return new OrderLineItem
                        {
                            OrderLine = line.LineSequenceNumber,
                            isReceived = true,
                            Quantity = line.ScannedQuantity,
                            RecordWeight = line.TotalWeight,
                            ActualWeight = line.ScannedWeight,
                            Rate = line.IsBad ? 0 : null,
                            InventoryDetail = new InventoryDetail
                            {
                                InventoryAssignment = new InventoryAssignment
                                {
                                    Items = new List<InventoryAssignmentItem>
                                {
                                    new()
                                    {
                                        InventoryStatus = new ReferenceValue
                                        {
                                            Id = "1"
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
                        };
                    }
                }).ToList()
            }
        };
    }
}
