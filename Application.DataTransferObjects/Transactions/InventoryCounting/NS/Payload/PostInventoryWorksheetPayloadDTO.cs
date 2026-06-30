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
        [JsonPropertyName("inventorystatus")]
        public int InventoryStatus { get; set; }

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }
    }

    public static PostInventoryWorksheetPayload PostInventoryWorksheet(
    List<InventoryWorksheetLineDTO> lines,
    int location)
    {
        return new PostInventoryWorksheetPayload
        {
            Subsidiary = 2,
            Account = 1,
            LastInDay = "T",
            Location = location,
            Department = 15,
            Class = 2,
            Lines = lines.Select(line =>
            {
                var details = new List<InventoryWorksheetInventoryDetail>();

                if (line.GoodScannedQuantity > 0)
                {
                    details.Add(new InventoryWorksheetInventoryDetail
                    {
                        InventoryStatus = 1,
                        Quantity = line.GoodScannedQuantity
                    });
                }

                if (line.BadScannedQuantity > 0)
                {
                    details.Add(new InventoryWorksheetInventoryDetail
                    {
                        InventoryStatus = 3,
                        Quantity = line.BadScannedQuantity
                    });
                }

                return new InventoryWorksheetLine
                {
                    ItemId = line.NetsuiteMaterialInternalId,
                    NewQuantity = line.GoodScannedQuantity + line.BadScannedQuantity,
                    InventoryDetail = details
                };
            }).ToList()
        };
    }
}

