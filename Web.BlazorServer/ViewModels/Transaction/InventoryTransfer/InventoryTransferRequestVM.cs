using Application.DataTransferObjects.Others;
using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.Transaction.Commons;

namespace Web.BlazorServer.ViewModels.Transaction.InventoryTransfer
{
    public class InventoryTransferRequestVM
    {
        public int? DocEntry { get; set; } = null;
        public int? DocNum { get; set; } = null;
        public DateTime DocDate { get; set; } = DateTime.Today;
        public WarehouseVM FromWarehouse { get; set; }
        public WarehouseVM ToWarehouse { get; set; }
        public TransferTypeVM TransferType { get; set; }

        public SchoolYearVM SchoolYear { get; set; } 
        public string? Remarks { get; set; }
        public string? PreparedBy { get; set; }
        public string? ApprovedBy { get; set; }
        public string? NotedBy { get; set; }

        public List<InventoryTransferRequestLineVM> Lines { get; set; } = [];

    }
}
