namespace Application.DataTransferObjects.Transactions.GoodsReturn;

public class GRRDataGridDTO
{
    public int DocEntry { get; set; }
    public int DocNum { get; set; }
    public int GRPODocEntry { get; set; }
    public int GRPODocNum { get; set; }
    public int PODocEntry { get; set; }
    public int PODocNum { get; set; }
    public DateTime DocDate { get; set; }
    public DateTime PoDocDate { get; set; }
    public DateTime DocDueDate { get; set; }
    public string CardCode { get; set; }
    public string CardName { get; set; }
    public string Remarks { get; set; }
}
