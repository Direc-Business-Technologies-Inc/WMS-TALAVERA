namespace Application.DataTransferObjects.Transactions.SalesReturn;

public class SalesReturnDataGridDTO
{
    public DateTime DocDate { get; set; }
    public int DocEntry { get; set; }
    public int DocNum { get; set; }
    public string CardCode { get; set; }
    public string CardName { get; set; }
    public string Remarks { get; set; }
}
