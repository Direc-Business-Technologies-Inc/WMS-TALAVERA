using System.ComponentModel;

namespace Application.DataTransferObjects.Others.NS;

public class PurchaseOrderDTO
{
    public string OrderNumber { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public int LineSequenceNumber { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public int MaterialInternalId { get; set; } //fk
    public string ParentCustomerEntityId { get; set; } = string.Empty;
    public int ParentCustomerInternalId { get; set; }
    public string CustomerEntityId { get; set; } = string.Empty;
    public int CustomerInternalId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public double? Rate { get; set; }
    public double? LineAmount { get; set; }
    public int NetsuiteOrderInternalId { get; set; } //fk
    public string Region { get; set; } = string.Empty;
    public string Cluster { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public decimal OrderTotalAmount { get; set; }
    public int AssignedQuantity { get; set; }
    public int PickedQuantity { get; set; }
    public DateTime NetsuiteOrderCreatedDate { get; set; }
    public DateTime NetsuiteOrderDocumentDate { get; set; }
    public DateTime NetsuiteOrderUpdatedDate { get; set; }
}
