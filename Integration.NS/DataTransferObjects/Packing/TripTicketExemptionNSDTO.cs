namespace Integration.NS.DataTransferObjects.Packing;

/// <summary>
/// Minimal shape returned by the authoritative trip-ticket exemption lookup.
/// <para>
/// ExemptionId is 0 when no active exemption record matches the order's
/// source/destination location pair.
/// </para>
/// </summary>
internal class TripTicketExemptionNSDTO
{
    public int ExemptionId { get; set; }
}