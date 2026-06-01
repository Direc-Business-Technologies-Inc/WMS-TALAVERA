namespace Integration.SAP.Entities.Transactional.Receiving
{
    public class PurchaseOrderHeaderSAPDTO
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public DateTime DocDate { get; set; }
        public DateTime DocDueDate { get; set; }
        public string DocStatus { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string SupplierContactPerson { get; set; }
        public string Remarks { get; set; }
        public string? PreparedBy { get; set; }
        public string? PONo { get; set; }
        public string? DRNo { get; set; }
        public string? Designation { get; set; }
        public string? ReceivedBy { get; set; }
        public string? ApprovedBy { get; set; }
        public string? NotedBy { get; set; }
        public string? SchoolYear { get; set; }
        public string? SINo { get; set; }
        public string? DeliveredBy { get; set; }
        public string? ReviewedBy { get; set; }
        public string? PurchaseType { get; set; }
        public string? ItemName { get; set; }
        public string? DocRemarks { get; set; }
        public string ItemGroupCodes { get; set; }
    }
}
