using Ardalis.GuardClauses;
using Domain.Entities.Entities.Transaction.InventoryCounting;
using Domain.ValueObjects;
using System.Security.Cryptography.X509Certificates;

namespace Domain.Entities.ValueObjects.Transaction;

public class InventoryCountingDocumentLineVO : ValueObject
{
    public string ItemCode { get; private set; }
    public string ItemName { get; private set; }
    public decimal ActualQuantity { get; private set; }
    public decimal Quantity { get; private set; }
    public string UoMCode { get; private set; }
    public decimal UoMValue { get; private set; }
    public string UoMName { get; private set; }
    public string? ISBN { get; private set; }


    public InventoryCountingDocumentLineVO() { }

    public InventoryCountingDocumentLineVO(string itemCode,
                                           string itemName,
                                           decimal actualQuantity,
                                           decimal quantity,
                                           string uomCode,
                                           decimal uomValue,
                                           string uomName)
    {
        ItemCode = Guard.Against.NullOrEmpty(itemCode, nameof(ItemCode), "Item Code cannot be null or empty");
        ItemName = Guard.Against.NullOrEmpty(itemName, nameof(ItemName), "Item Name cannot be null or empty");
        Quantity = Guard.Against.Null(quantity, nameof(Quantity), "Item Quantity cannot be null");
        UoMCode = Guard.Against.NullOrEmpty(uomCode, nameof(UoMCode), "UoM Code cannot be null or empty");
        UoMValue = Guard.Against.Null(uomValue, nameof(UoMValue), "UoM Value cannot be null");
        UoMValue = Guard.Against.NegativeOrZero(uomValue, nameof(UoMValue), "UoM Value cannot be negative or zero");
        UoMName = Guard.Against.NullOrEmpty(uomName, nameof(UoMName), "UoM Name cannot be null or empty");
    }

    public InventoryCountingDocumentLineVO UpdateActualQuantity(decimal actualQuantity)
    {
        ActualQuantity = Guard.Against.Negative(actualQuantity, nameof(ActualQuantity), "Actual Quantity cannot be negative");
        return this;
    }

    public InventoryCountingDocumentLineVO SetISBN(string? isbn)
    {
        ISBN = isbn;
        return this;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return ItemCode;
        yield return ItemName;
        yield return ActualQuantity;
        yield return Quantity;
        yield return UoMCode;
        yield return UoMValue;
        yield return UoMName;


    }
}
