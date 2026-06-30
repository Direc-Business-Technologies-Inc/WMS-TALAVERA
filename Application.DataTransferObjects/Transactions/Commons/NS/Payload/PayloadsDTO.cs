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
    public InventoryDetail? InventoryDetail { get; set; } = null;
}

public class InventoryDetail
{
    [JsonPropertyName("inventoryAssignment")]
    public InventoryAssignment? InventoryAssignment { get; set; } = null;

    [JsonPropertyName("inventoryAssignmentList")]
    public InventoryAssignmentList? InventoryAssignmentList { get; set; } = null;
}

public class InventoryAssignmentList
{
    [JsonPropertyName("inventoryAssignment")]
    public InventoryAssignment? InventoryAssignment { get; set; } = null;
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
    public ReferenceValue? BinNumber { get; set; } = null;

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

public class CountItemContainer
{
    [JsonPropertyName("items")]
    public List<CountLineItem> Items { get; set; } = new();
}

public class CountLineItem
{
    [JsonPropertyName("countLine")]
    public int CountLine { get; set; }

    [JsonPropertyName("countQuantity")]
    public decimal? CountQuantity { get; set; }

    [JsonPropertyName("countDetail")]
    public CountDetail? CountDetail { get; set; }
}

public class CountDetail
{
    [JsonPropertyName("inventoryDetail")]
    public CountInventoryDetail CountInventoryDetail { get; set; } = new();
}

public class CountInventoryDetail
{
    [JsonPropertyName("items")]
    public List<CountInventoryDetailItem> Items { get; set; } = [];
}

public class CountInventoryDetailItem
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("quantity")]
    public decimal? Quantity { get; set; }

    [JsonPropertyName("inventoryStatus")]
    public ReferenceValue InventoryStatus { get; set; } = new();
}