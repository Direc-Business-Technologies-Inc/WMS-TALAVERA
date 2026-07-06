using System.Text.Json.Serialization;

namespace Application.DataTransferObjects.Transactions.InventoryCounting.NS.Payload;

public class PostInventoryWorksheetPayload
{
    [JsonPropertyName("subsidiary")]
    public int Subsidiary { get; set; }

    [JsonPropertyName("account")]
    public int Account { get; set; }

    [JsonPropertyName("lastinday")]
    public string LastInDay { get; set; }

    [JsonPropertyName("location")]
    public int Location { get; set; }

    [JsonPropertyName("department")]
    public int Department { get; set; }

    [JsonPropertyName("class")]
    public int Class { get; set; }

    [JsonPropertyName("lines")]
    public List<InventoryWorksheetLine> Lines { get; set; } = new();

    public class InventoryWorksheetLine
    {
        [JsonPropertyName("invtid")]
        public int ItemId { get; set; }

        [JsonPropertyName("newqty")]
        public decimal NewQuantity { get; set; }

        [JsonPropertyName("inventoryDetail")]
        public List<InventoryWorksheetInventoryDetail> InventoryDetail { get; set; } = new();
    }

    public class InventoryWorksheetInventoryDetail
    {
        [JsonPropertyName("bin")]
        public int? Bin { get; set; }
        [JsonPropertyName("inventorystatus")]
        public int InventoryStatus { get; set; }

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }
    }

    public static PostInventoryWorksheetPayload PostInventoryWorksheet(
    List<InventoryWorksheetLineDTO> icItems,
    int location,
    int subsidiary)
    {
        var lines = icItems
            .Where(x => x.GoodScannedQuantity > 0 || x.BadScannedQuantity > 0)
            .GroupBy(x => x.NetsuiteMaterialInternalId)
            .Select(itemGroup =>
            {
                var inventoryDetails = itemGroup
                    .SelectMany(line =>
                    {
                        var details = new List<InventoryWorksheetInventoryDetail>();

                        if (line.GoodScannedQuantity > 0)
                        {
                            details.Add(new InventoryWorksheetInventoryDetail
                            {
                                Bin = line.NetsuiteBinInternalId != 0
                                    ? line.NetsuiteBinInternalId
                                    : null,
                                InventoryStatus = 1, // Good
                                Quantity = line.GoodScannedQuantity
                            });
                        }

                        if (line.BadScannedQuantity > 0)
                        {
                            details.Add(new InventoryWorksheetInventoryDetail
                            {
                                Bin = line.NetsuiteBinInternalId != 0
                                    ? line.NetsuiteBinInternalId
                                    : null,
                                InventoryStatus = 3, // Bad
                                Quantity = line.BadScannedQuantity
                            });
                        }

                        return details;
                    })
                    .ToList();

                return new InventoryWorksheetLine
                {
                    ItemId = itemGroup.Key,
                    NewQuantity = inventoryDetails.Sum(x => x.Quantity),
                    InventoryDetail = inventoryDetails
                };
            })
            .Where(x => x.InventoryDetail.Count != 0)
            .ToList();

        return new PostInventoryWorksheetPayload
        {
            Subsidiary = subsidiary,
            Account = 1,
            LastInDay = "T",
            Location = location,
            Department = 15,
            Class = 2,
            Lines = lines
        };
    }
}

