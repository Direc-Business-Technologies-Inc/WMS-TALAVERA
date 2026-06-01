using Application.DataTransferObjects.Others;

namespace Application.DataTransferObjects.Transactions.InventoryTransfer
{
    public class InventoryTransferDataGridDTO
    {
        public int DocEntry {  get; set; }
        public int DocNum { get; set; }
        public DateTime DocDate { get; set; }
        public string FromWhsName { get; set; }
        public string ToWhsName { get; set; }
        public string Remarks { get; set; }
        public string PreparedBy { get; set; }
    }
}
