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
        return null;
        // Make it nullable if its not included in json
        //return new PurchaseOrderIRPayloadDTO
        //{
        //    ReceivingCategory = receivingCategory, // 1 is Good , 2 is Bad
        //    Item = new ItemContainer
        //    {
        //        Items = lines.Select(line =>
        //        {
        //            if (line.ScannedQuantity == 0)
        //            {
        //                return new OrderLineItem
        //                {
        //                    OrderLine = line.LineSequenceNumber,
        //                    isReceived = false
        //                };
        //            }
        //            else
        //            {
        //                return new OrderLineItem
        //                {
        //                    OrderLine = line.LineSequenceNumber,
        //                    isReceived = true,
        //                    Quantity = line.ScannedQuantity,
        //                    RecordWeight = line.TotalWeight,
        //                    ActualWeight = line.ScannedWeight,
        //                    Rate = line.IsBad ? 0 : null,
        //                    InventoryDetail = new InventoryDetail
        //                    {
        //                        InventoryAssignment = new InventoryAssignment
        //                        {
        //                            Items = new List<InventoryAssignmentItem>
        //                        {
        //                            new()
        //                            {
        //                                InventoryStatus = new ReferenceValue
        //                                {
        //                                    Id = line.IsBad ? "3" : "1"
        //                                },
        //                                BinNumber = new ReferenceValue
        //                                {
        //                                    Id = line.IsLocationUsedBin
        //                                        ? (line.VendorBinAssignmentId != 0
        //                                                ? line.VendorBinAssignmentId.ToString()
        //                                                : line.NetsuiteMaterialPrefferedBinId.ToString())
        //                                        : null
        //                                },
        //                                Quantity = line.ScannedQuantity
        //                            }
        //                        }
        //                        }
        //                    }
        //                };
        //            }
        //        }).ToList()
        //    }
        //};
    }
}
