using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Payload;
using System.Text.Json.Serialization;

namespace Application.DataTransferObjects.Transactions.Receiving.NS.Payload;
public class ReturnsIRPayloadDTO
{
    [JsonPropertyName("custbody_dbti_created_in_wms")]
    public bool CreatedInWms { get; set; } = true;

    [JsonPropertyName("custbody_dbti_receiving_category")]
    public int ReceivingCategory { get; set; }

    [JsonPropertyName("custbody_dbti_received_by")]
    public int EmployeeId { get; set; }

    [JsonPropertyName("memo")]
    public string Memo { get; set; } = string.Empty;

    [JsonPropertyName("itemfulfillment")]
    public int ItemFulfillmentId { get; set; }

    [JsonPropertyName("item")]
    public ItemContainer Item { get; set; } = new();

    public static ReturnsIRPayloadDTO CreateForItemReceipt(
        List<PostReturnsDTO> lines,
        int receivingCategory,
        int ifOrderId,
        int userId,
        string remarks
        )
    {
        // Make it nullable if its not included in json
        return new ReturnsIRPayloadDTO
        {
            ReceivingCategory = receivingCategory,
            EmployeeId = userId,
            ItemFulfillmentId = ifOrderId,
            Memo = remarks,
            Item = new ItemContainer
            {
                Items = lines.Select(line =>
                {
                    if (line.ScannedQuantity == 0)
                    {
                        // SaveReturnsItemReceipt always posts with
                        // ?replace=item.inventoryDetail.inventoryAssignment, so the sub-resource has
                        // to exist on EVERY item entry. A partially scanned returns receipt otherwise
                        // fails with [INVALID_PARAMETER] "Resource or sub-resource 'InventoryDetail'
                        // ... was not found in the request body". Zero-qty fillers carry an EMPTY
                        // assignment, matching the fulfillment builders' proven shape.
                        return new OrderLineItem
                        {
                            OrderLine = line.LineSequenceNumber,
                            isReceived = false,
                            InventoryDetail = new InventoryDetail
                            {
                                InventoryAssignment = new InventoryAssignment
                                {
                                    Items = []
                                }
                            }
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
