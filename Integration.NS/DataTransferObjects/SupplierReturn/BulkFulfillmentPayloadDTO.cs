using System.Text.Json.Serialization;

namespace Integration.NS.DataTransferObjects.SupplierReturn;

public record BulkFulfillmentPayloadDTO
{
    public BulkFulfillmentPayloadDTO(DateOnly date, IEnumerable<int> returnIds, string? memo)
    {
        Date = date;
        Memo = memo;
        Returns = new(returnIds.Select(id => new RecordReference(
            id.ToString(System.Globalization.CultureInfo.InvariantCulture))).ToArray());
    }

    [JsonPropertyName("custrecord_bf_date")]
    public DateOnly Date { get; }

    [JsonPropertyName("custrecord_bf_memo")]
    public string? Memo { get; }

    [JsonPropertyName("custrecord_bf_return_to_supplier_ids")]
    public ReferenceCollection Returns { get; }

    [JsonPropertyName("custrecord_bf_status")]
    public RecordReference Status { get; } = new("1");

    public record RecordReference([property: JsonPropertyName("id")] string Id);
    public record ReferenceCollection([property: JsonPropertyName("items")] RecordReference[] Items);
}
