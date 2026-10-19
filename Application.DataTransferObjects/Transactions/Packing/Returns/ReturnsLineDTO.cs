namespace Application.DataTransferObjects.Transactions.Packing.Returns;

public class ReturnsLineDTO
{
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public string UoM { get; set; } = string.Empty;
    public string Warehouse { get; set; } = string.Empty;
    public decimal QuantityAvailable { get; set; }
    public decimal QuantityPlanned { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityBackOrdered { get; set; }

    /// <summary>
    /// Raw internal id of the header's transfer category (3 = Returns Good, 4 = Returns Bad).
    /// Carried per line because the packing category guard in PostReturnsIFCmd reads it there.
    /// </summary>
    public int TransferCategory { get; set; }
}
