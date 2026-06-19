namespace Web.BlazorServer.ViewModels.Others;

public class ItemUnitVM
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Abbreviation { get; set; } = string.Empty;
    public decimal ConversionRate { get; set; }
}
