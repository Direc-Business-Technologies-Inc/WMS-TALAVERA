using Shared.Libraries.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.Receiving.NS.Payload;

public class PurchaseOrderPayloadDTO
{
    [JsonPropertyName("custbody_dbti_receiving_category")]
    public int ReceivingCategory { get; set; }
    [JsonPropertyName("item")]
    public ItemContainer Item { get; set; } = new();

    public static PurchaseOrderPayloadDTO CreateForItemReceipt(
        List<PurchaseOrderLineVM> lines,
        int receivingCategory)
    {
        // Make it nullable if its not included in json
        return new PurchaseOrderPayloadDTO
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

                    return new OrderLineItem
                    {
                        OrderLine = line.LineSequenceNumber,
                        isReceived = true,
                        Quantity = line.ScannedQuantity,
                        RecordWeight = line.TotalWeight,
                        ActualWeight = line.ScannedWeight,
                        Rate = line.IsBad ? 0 : null,
                        Location = line.IsBad ? line.NetsuiteSubsidiaryDefaultBOInternalId : null,
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
                                            Id = line.IsBad ? "2" : "1"
                                        },
                                        BinNumber = new ReferenceValue
                                        {
                                            Id = line.IsLocationUsedBin
                                                ? (line.IsBad
                                                    ? "5"
                                                    : line.VendorBinAssignmentId != 0
                                                        ? line.VendorBinAssignmentId.ToString()
                                                        : line.NetsuiteMaterialPrefferedBinId.ToString())
                                                : null
                                        },
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

public class ItemContainer
{
    [JsonPropertyName("items")]
    public List<OrderLineItem> Items { get; set; } = new();
}

public class OrderLineItem
{
    [JsonPropertyName("orderLine")]
    public int OrderLine { get; set; }

    [JsonPropertyName("itemreceive")]
    public bool? isReceived { get; set; }

    [JsonPropertyName("quantity")]
    public decimal? Quantity { get; set; }

    [JsonPropertyName("location")]
    public int? Location { get; set; }

    [JsonPropertyName("rate")]
    public decimal? Rate { get; set; }

    [JsonPropertyName("custcol_dbti_record_weight")]
    public decimal RecordWeight { get; set; }

    [JsonPropertyName("custcol_dbti_actual_weight")]
    public decimal ActualWeight { get; set; }

    [JsonPropertyName("inventoryDetail")]
    public InventoryDetail InventoryDetail { get; set; } = new();
}

public class InventoryDetail
{
    [JsonPropertyName("inventoryAssignment")]
    public InventoryAssignment InventoryAssignment { get; set; } = new();
}

public class InventoryAssignment
{
    [JsonPropertyName("items")]
    public List<InventoryAssignmentItem> Items { get; set; } = new();
}

public class InventoryAssignmentItem
{
    [JsonPropertyName("inventoryStatus")]
    public ReferenceValue InventoryStatus { get; set; } = new();

    [JsonPropertyName("binNumber")]
    public ReferenceValue BinNumber { get; set; } = new();

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }
}

public class ReferenceValue
{
    [JsonPropertyName("id")]
    public string? Id { get; set; } = string.Empty;
}
