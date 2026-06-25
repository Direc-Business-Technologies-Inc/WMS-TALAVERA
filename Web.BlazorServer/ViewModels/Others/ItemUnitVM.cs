namespace Web.BlazorServer.ViewModels.Others;

public class ItemUnitVM : IComparable<ItemUnitVM>
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Abbreviation { get; set; } = string.Empty;
    public decimal ConversionRate { get; set; }
    public int CompareTo(ItemUnitVM? other)
    {
        if (other == null) return 1;

        return Name.CompareTo(other.Name);
    }
}
