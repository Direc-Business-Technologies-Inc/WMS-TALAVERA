using System.Text.Json.Serialization;

namespace Application.DataTransferObjects.Transactions;

/// <summary>Counts of eligible receipts or fulfillments, not source documents.</summary>
public sealed class PrintStatusCountsDTO
{
    [JsonPropertyName("forprinting")]
    public int ForPrinting { get; set; }

    [JsonPropertyName("printed")]
    public int Printed { get; set; }
}
