using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.Commons.NS.Payload;

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
    public decimal? RecordWeight { get; set; }

    [JsonPropertyName("custcol_dbti_actual_weight")]
    public decimal? ActualWeight { get; set; }

    [JsonPropertyName("inventoryDetail")]
    public InventoryDetail? InventoryDetail { get; set; } = new();
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

public class ReferenceValues
{
    [JsonPropertyName("ids")]
    public List<string> Ids { get; set; } = [];
}