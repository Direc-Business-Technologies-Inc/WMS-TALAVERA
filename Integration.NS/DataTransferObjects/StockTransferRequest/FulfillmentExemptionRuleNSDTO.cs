using System.Text.Json.Serialization;
using System.Text.Json;

namespace Integration.NS.DataTransferObjects.StockTransferRequest;

internal sealed class FulfillmentExemptionRuleNSDTO
{
    [JsonPropertyName("RuleId")]
    public long? RuleId { get; set; }

    [JsonPropertyName("TransferCategoryId")]
    public JsonElement TransferCategoryId { get; set; }

    [JsonPropertyName("FromLocationId")]
    public JsonElement FromLocationId { get; set; }

    [JsonPropertyName("ToLocationId")]
    public JsonElement ToLocationId { get; set; }

    [JsonPropertyName("AutomatedPackingValue")]
    public JsonElement AutomatedPackingValue { get; set; }

    [JsonPropertyName("AutomatedShippingValue")]
    public JsonElement AutomatedShippingValue { get; set; }

    [JsonPropertyName("LabelTemplateId")]
    public JsonElement LabelTemplateId { get; set; }

    [JsonPropertyName("IsInactive")]
    public string? IsInactive { get; set; }
}
