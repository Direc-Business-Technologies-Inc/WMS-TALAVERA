using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.ViewModels.Transaction.InventoryTransfer
{
    public class InventoryTransferRequestDraftDataGridVM
    {
        public int DraftEntry {  get; set; }
        public int DocNum { get; set; }
        public DateTime DocDate { get; set; }
        public string FromWhsName { get; set; }
        public string ToWhsName { get; set; }
        public string Remarks { get; set; }
        public string PreparedBy { get; set; }

    }
}