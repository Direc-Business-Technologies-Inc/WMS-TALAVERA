using Ardalis.GuardClauses;

namespace Domain.Entities.Entities.Transaction.Common;

public abstract class ItemDEM
{
    public string ItemCode { get; private set; }
    public string ItemName { get; private set; }
    public decimal Quantity { get; private set; }
    public string UoMCode { get; private set; }
    public decimal UoMValue { get; private set; }
    public string UoMName { get; private set; }

    protected ItemDEM() { }

    protected ItemDEM(string itemCode,
                      string itemName,
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

    public ItemDEM UpdateItemCode(string itemCode)
    {
        ItemCode = Guard.Against.NullOrEmpty(itemCode, nameof(ItemCode), "Item Code cannot be null or empty");

        return this;
    }

    public ItemDEM UpdateItemName(string itemName)
    {
        ItemName = Guard.Against.NullOrEmpty(itemName, nameof(ItemName), "Item Name cannot be null or empty");

        return this;
    }

    public ItemDEM UpdateUoMCode(string uomCode)
    {
        UoMCode = Guard.Against.NullOrEmpty(uomCode, nameof(UoMCode), "UoM Code cannot be null or empty");

        return this;
    }

    public ItemDEM UpdateUoMValue(decimal uomValue)
    {
        UoMValue = Guard.Against.Null(uomValue, nameof(UoMValue), "UoM Value cannot be null");
        UoMValue = Guard.Against.NegativeOrZero(uomValue, nameof(UoMValue), "UoM Value cannot be negative or zero");

        return this;
    }

    public ItemDEM UpdateUoMName(string uomName)
    {
        UoMName = Guard.Against.NullOrEmpty(uomName, nameof(UoMName), "UoM Name cannot be null or empty");

        return this;
    }
}
