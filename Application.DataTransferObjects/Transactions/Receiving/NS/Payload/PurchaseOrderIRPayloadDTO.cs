using Application.DataTransferObjects.Transactions.Commons.NS.Payload;
using System.Text.Json.Serialization;

namespace Application.DataTransferObjects.Transactions.Receiving.NS.Payload;

public class PurchaseOrderIRPayloadDTO
{
    [JsonPropertyName("custbody_dbti_created_in_wms")]
    public bool CreatedInWms { get; set; } = true;

    [JsonPropertyName("custbody_dbti_receiving_category")]
    public int ReceivingCategory { get; set; }

    [JsonPropertyName("custbody_dbti_received_by")]
    public int EmployeeId { get; set; }

    [JsonPropertyName("memo")]
    public string Memo { get; set; } = string.Empty;

    [JsonPropertyName("item")]
    public ItemContainer Item { get; set; } = new();

    public static PurchaseOrderIRPayloadDTO CreateForItemReceipt(
        List<PostPurchaseOrderDTO> lines,
        int userId,
        int receivingCategory,
        string remarks
        )
    {
        bool isGoodPost = receivingCategory == 1;

        // Make it nullable if its not included in json
        return new PurchaseOrderIRPayloadDTO
        {
            ReceivingCategory = receivingCategory, // 1 is Good , 2 is Bad
            EmployeeId = userId,
            Memo = remarks,
            Item = new ItemContainer
            {
                Items = lines.Select(line =>
                {
                    bool isReceived = line.ScannedQuantity > 0 && (isGoodPost ? !line.IsBad : line.IsBad);

                    if (!isReceived)
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
                        PhysicaQuantity = line.PhysicalQuantity,
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
                                    BinNumber = !line.IsLocationUsedBin ? null :
                                        line.VendorBinAssignmentId != 0
                                        ? new ReferenceValue
                                        {
                                            Id = line.VendorBinAssignmentId.ToString()
                                        }
                                        : line.NetsuiteMaterialPrefferedBinId != 0
                                        ? new ReferenceValue
                                        {
                                            Id = line.NetsuiteMaterialPrefferedBinId.ToString()
                                        }
                                        : null,
                                    Quantity = line.ScannedQuantity
                                }
                            }
                            }
                        }
                    };
                }).ToList()
            }
        };
    }
}
