namespace Integration.NS.DataTransferObjects.Packing.Returns;

public class ReturnPackingLineNSDTO
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
    /// The packing line query used to filter on it without projecting it, so
    /// <see cref="Application.DataTransferObjects.Transactions.Commons.NS.ReturnsLineDTO.TransferCategory"/>
    /// stayed 0 and the category guard in PostReturnsIFCmd failed every Good line.
    /// Selected raw rather than via BUILTIN.DF because the guard compares against the id.
    /// </summary>
    public int TransferCategory { get; set; }
}
