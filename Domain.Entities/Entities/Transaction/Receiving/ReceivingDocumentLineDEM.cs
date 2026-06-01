using Ardalis.GuardClauses;
using Domain.Entities.Entities.Transaction.Common;
using Domain.Entities.ValueObjects.Others;

namespace Domain.Entities.Entities.Transaction.Receiving;

public class ReceivingDocumentLineDEM : ItemDEM
{

    public WarehouseVO Warehouse { get; private set; }
    public decimal TargetQuantity { get; private set; }
    public decimal OpenQuantity { get; private set; }
    public string VatGroup { get; private set; }

    public ReceivingDocumentLineDEM() { }

    public ReceivingDocumentLineDEM(WarehouseVO warehouse,
                                    decimal targetQty,
                                    string vatGroup,
                                    string itemCode,
                                    string itemName,
                                    decimal quantity,
                                    string uomCode,
                                    decimal uomValue,
                                    string uomName) : base(itemCode,
                                                           itemName,
                                                           quantity,
                                                           uomCode,
                                                           uomValue,
                                                           uomName)
    {
        Guard.Against.NegativeOrZero(quantity, nameof(Quantity), "Quantity cannot be negative or zero");

        Warehouse = Guard.Against.Null(warehouse, nameof(Warehouse), "Warehouse cannot be null");
        TargetQuantity = Guard.Against.Null(targetQty, nameof(TargetQuantity), "Target Quantity cannot be null");
        TargetQuantity = Guard.Against.NegativeOrZero(targetQty, nameof(TargetQuantity), "Target Quantity cannot be negative or zero");
        VatGroup = Guard.Against.NullOrEmpty(vatGroup, nameof(VatGroup), "Vat Group cannot be null or empty");
    }

    public ReceivingDocumentLineDEM UpdaterWarehouse(WarehouseVO warehouse)
    {
        Warehouse = Guard.Against.Null(warehouse, nameof(Warehouse), "Warehouse cannot be null");
        return this;
    }

    public ReceivingDocumentLineDEM UpdateTargetQuantity(decimal targetQty)
    {
        TargetQuantity = Guard.Against.Null(targetQty, nameof(TargetQuantity), "Target Quantity cannot be null");
        TargetQuantity = Guard.Against.NegativeOrZero(targetQty, nameof(TargetQuantity), "Target Quantity cannot be negative or zero");
        return this;
    }

    public ReceivingDocumentLineDEM UpdateVatGroup(string vatGroup)
    {
        VatGroup = Guard.Against.NullOrEmpty(vatGroup, nameof(VatGroup), "Vat Group cannot be null or empty");
        return this;
    }
}
