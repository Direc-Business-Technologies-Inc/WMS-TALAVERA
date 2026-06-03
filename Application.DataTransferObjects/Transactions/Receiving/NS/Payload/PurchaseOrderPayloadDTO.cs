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
    [JsonPropertyName("item")]
    public ItemContainer Item { get; set; } = new();

    //public static PurchaseOrderPayloadDTO CreateForFulfillment(
    //    int orderLine,
    //    decimal quantity,
    //    string inventoryStatusId,
    //    string binNumberId)
    //{
    //    return new PurchaseOrderPayloadDTO
    //    {
    //        Item = new ItemContainer
    //        {
    //            Items = new List<OrderLineItem>
    //            {
    //                new()
    //                {
    //                    OrderLine = orderLine,
    //                    InventoryDetail = new InventoryDetail
    //                    {
    //                        InventoryAssignment = new InventoryAssignment
    //                        {
    //                            Items = new List<InventoryAssignmentItem>
    //                            {
    //                                new()
    //                                {
    //                                    InventoryStatus = new ReferenceValue
    //                                    {
    //                                        Id = inventoryStatusId
    //                                    },
    //                                    BinNumber = new ReferenceValue
    //                                    {
    //                                        Id = binNumberId
    //                                    },
    //                                    Quantity = quantity
    //                                }
    //                            }
    //                        }
    //                    }
    //                }
    //            }
    //        }
    //    };
    public static PurchaseOrderPayloadDTO CreateForFulfillment(
        List<PurchaseOrderLineVM> lines)
    {
        return new PurchaseOrderPayloadDTO
        {
            Item = new ItemContainer
            {
                Items = lines.Select(line => new OrderLineItem
                {
                    OrderLine = line.LineSequenceNumber,
                    Quantity = line.ScannedQuantity / line.UoMRate,
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
                                        Id = line.VendorBinAssignmentId.ToString()
                                    },
                                    Quantity = line.ScannedQuantity / line.UoMRate
                                }
                            }
                        }
                    }
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

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

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
    public string Id { get; set; } = string.Empty;
}
