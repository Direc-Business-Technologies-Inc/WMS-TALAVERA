using Ardalis.GuardClauses;
using Domain.Entities.Entities.Transaction.Common;

namespace Domain.Entities.Entities.Transaction.InventoryCounting;

public class InventoryCountingDocumentLineDEM : ItemDEM
{
    public decimal ActualQuantity { get; private set; }
    public Guid InventoryCountingDocumentId { get; private set; }

    public InventoryCountingDocumentLineDEM() { }

    public InventoryCountingDocumentLineDEM(
        Guid inventoryCountingDocumentId,
        string itemCode,
        string itemName,
        decimal actualQuantity,
        string uomCode,
        decimal uomValue,
        string uomName) : base(itemCode, itemName, actualQuantity, uomCode, uomValue, uomName)
    {
        InventoryCountingDocumentId = Guard.Against.NullOrEmpty(inventoryCountingDocumentId, nameof(InventoryCountingDocumentId), "Document Id cannot be empty");
        ActualQuantity = Guard.Against.Negative(actualQuantity, nameof(ActualQuantity), "Actual Quantity cannot be negative");
    }

    public InventoryCountingDocumentLineDEM UpdateActualQuantity(decimal actualQuantity)
    {
        ActualQuantity = Guard.Against.Negative(actualQuantity, nameof(ActualQuantity), "Actual Quantity cannot be negative");
        return this;
    }


    public InventoryCountingDocumentLineDEM SetReference(Guid id)
    {
        InventoryCountingDocumentId = Guard.Against.NullOrEmpty(id, nameof(InventoryCountingDocumentId), "Reference Document cannot be null or empty");
        return this;
    }
}
