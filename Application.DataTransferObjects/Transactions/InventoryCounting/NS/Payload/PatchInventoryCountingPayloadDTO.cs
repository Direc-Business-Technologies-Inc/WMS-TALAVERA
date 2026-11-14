using Application.DataTransferObjects.Transactions.Commons.NS.Payload;
using System.Text.Json.Serialization;

namespace Application.DataTransferObjects.Transactions.InventoryCounting.NS.Payload;

public class PatchInventoryCountingPayloadDTO
{
    [JsonPropertyName("item")]
    public CountItemContainer Item { get; set; } = new();

    public static PatchInventoryCountingPayloadDTO PatchInventoryCounting(
    List<PatchInventoryCountingDTO> lines)
    {
        var items = lines
            .Where(line => line.IsCounted || line.ScannedQuantity > 0)
            .GroupBy(line => line.LineSequenceNumber)
            .Select(group =>
            {
                var good = group.FirstOrDefault(line => !line.IsBad);
                var bad = group.FirstOrDefault(line => line.IsBad);

                var goodQuantity = good?.ScannedQuantity ?? 0;
                var badQuantity = bad?.ScannedQuantity ?? 0;

                var details = new List<CountInventoryDetailItem>();

                // A line counted as zero still needs one detail row, otherwise NetSuite keeps the
                // previous system quantity instead of recording the zero the operator entered.
                if (goodQuantity > 0 || badQuantity <= 0)
                {
                    details.Add(new CountInventoryDetailItem
                    {
                        Id = good?.NetsuiteInventoryDetailInternalId is > 0
                            ? good.NetsuiteInventoryDetailInternalId
                            : null,
                        Quantity = goodQuantity,
                        InventoryStatus = new ReferenceValue { Id = InventoryStatusMap.Good }
                    });
                }

                if (badQuantity > 0)
                {
                    details.Add(new CountInventoryDetailItem
                    {
                        Id = bad?.NetsuiteInventoryDetailInternalId is > 0
                            ? bad.NetsuiteInventoryDetailInternalId
                            : null,
                        Quantity = badQuantity,
                        InventoryStatus = new ReferenceValue { Id = InventoryStatusMap.Bad }
                    });
                }

                return new CountLineItem
                {
                    CountLine = group.Key,
                    CountQuantity = goodQuantity + badQuantity,
                    CountDetail = new CountDetail
                    {
                        CountInventoryDetail = new CountInventoryDetail { Items = details }
                    }
                };
            })
            .ToList();

        return new PatchInventoryCountingPayloadDTO
        {
            Item = new CountItemContainer { Items = items }
        };
    }
}
