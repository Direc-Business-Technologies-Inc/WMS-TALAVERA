namespace Application.DataTransferObjects.Transactions.SalesReturn.SAP;

public class SalesReturnHeaderSAPDTO
{
    public int DocEntry {  get; set; }
    public int DocNum { get; set; }
    public DateTime DocDate {  get; set; }
    public DateTime DocDueDate {  get; set; }
    public string CardCode {  get; set; }
    public string CardName {  get; set; }
    public string ContactPerson {  get; set; }
    public string NumAtCard {  get; set; }
    public string SchoolYear {  get; set; }
    public string ReturnType {  get; set; }
    public string PURNo {  get; set; }
    public string DRNo {  get; set; }
    public string SONo {  get; set; }
    public string SINo {  get; set; }
    public string Designation {  get; set; }
    public string DocRemarks {  get; set; }
    public string ReturnedBy {  get; set; }
    public string PickBy {  get; set; }
    public string PreparedBy {  get; set; }
    public string CheckedBy {  get; set; }
    public string NotedBy {  get; set; }
    public string ApprovedBy {  get; set; }
}
