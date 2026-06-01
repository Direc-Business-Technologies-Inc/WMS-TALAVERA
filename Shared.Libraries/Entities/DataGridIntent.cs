namespace Shared.Entities;

public class DataGridIntent
{
    public List<AppFilterDescriptor> Filters { get; set; } = new();
    public List<AppSortDescriptor> Sorts { get; set; } = new();
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 10;
}
