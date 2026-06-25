using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.Receiving.NS.Payload;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Transactions.Receiving;

public class ItemReceiptTransformPayload
{
    [JsonPropertyName("custbody_dbti_receiving_category")]
    public int? ReceivingCategory { get; internal set; }

    [JsonPropertyName("item")]
    public ItemContainer? Item { get; internal set; }

    [JsonPropertyName("memo")]
    public string? Memo { get; internal set; } = "Item receipt create via WMS";

    [JsonPropertyName("transferOrderId")]
    public int? TransferOrderId { get; set; }

    [JsonPropertyName("transferCategory")]
    public int? TransferCategory { get; set; }

    [JsonPropertyName("lines")]
    public List<LinesContainer>? Lines { get; set; }

    private static string? GetLineBinNumber(ItemReceiptLineDTO dto, bool isGood, int vendorPrefferedBin)
    {
        if (!dto.IsLocationBinUsed) return null;
        if (!isGood) return "5";

        return vendorPrefferedBin != 0 ? $"{vendorPrefferedBin}" : $"{dto.PrefferedBinAssignmentId}";
    }

    private static ItemContainer GeneratePOLines(ItemReceiptDTO dto, bool isGood)
    {
        ItemContainer item = new();
        foreach (var line in dto.Lines)
        {
            if (line.QuantityGood == 0 || !line.IsReceived) item.Add(new(line.LineNumber) { isReceived = false });
            else
            {
                var x = new ItemContainerItems(line.LineNumber)
                {
                    Quantity = line.QuantityGood,
                    isReceived = true,
                    Rate = isGood ? null : 0,
                    ActualWeight = line.WeightActual,
                    Location = isGood ? null : dto.DefaultBO
                };
                x.InventoryDetail = new();
                var binId = GetLineBinNumber(line, isGood, dto.VendorPrefferedBin);
                x.InventoryDetail.AddAssignmentItem(new()
                {
                    InventoryStatus = new(isGood ? "1" : "3"), //magic
                    BinNumber = binId is null ? null : new(binId),
                    Quantity = line.QuantityGood
                });
                item.Add(x);
            }
        }
        return item;
    }
}


public class LinesContainer
{
    [JsonPropertyName("orderLine")]
    public int OrderLine { get; set; }

    [JsonPropertyName("quantity")]
    public decimal? Quantity { get; set; }

    [JsonPropertyName("rate")]
    public decimal? Rate { get; set; }
}

public class ItemContainer
{
    [JsonPropertyName("items")]
    public List<ItemContainerItems> Items { get; internal set; } = [];

    public void Add(ItemContainerItems item)
    {
        Items.Add(item);
    }
}

public class ItemContainerItems(int? lineNumber = null)
{

    [JsonPropertyName("location")]
    public int? Location { get; internal set; }

    [JsonPropertyName("itemreceive")]
    public bool? isReceived { get; internal set; }

    [JsonPropertyName("orderLine")]
    public int? LineNumber { get; internal set; } = lineNumber;

    [JsonPropertyName("custcol_dbti_actual_weight")]
    public decimal? ActualWeight { get; set; }

    [JsonPropertyName("quantity")]
    public decimal? Quantity { get; internal set; }

    [JsonPropertyName("rate")]
    public decimal? Rate { get; internal set; }

    [JsonPropertyName("inventoryDetail")]
    public InventoryDetail? InventoryDetail { get; set; }
}

public class InventoryAssignmentItem
{
    [JsonPropertyName("inventoryStatus")]
    public IdReference? InventoryStatus { get; internal set; }

    [JsonPropertyName("binNumber")]
    public IdReference? BinNumber { get; internal set; }

    [JsonPropertyName("quantity")]
    public decimal? Quantity { get; internal set; }
}

public class IdReference(string? id = null)
{
    [JsonPropertyName("id")]
    public string? Id { get; internal set; } = id;
}

public class InventoryAssignment
{
    [JsonPropertyName("items")]
    public List<InventoryAssignmentItem> Items { get; set; } = [];
}

public class InventoryDetail
{

    [JsonPropertyName("inventoryAssignment")]
    public InventoryAssignment InventoryAssignment { get; set; } = new();

    public void AddAssignmentItem(InventoryAssignmentItem item) => InventoryAssignment.Items.Add(item);
}

