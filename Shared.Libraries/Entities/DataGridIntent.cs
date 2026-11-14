namespace Shared.Entities;

public class DataGridIntent
{
    // Execution metadata travels with internal requests, never as business filters
    // or as part of a serialized public contract/cache key.
    [System.Text.Json.Serialization.JsonIgnore]
    public GridReadPolicy ReadPolicy { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public CancellationToken CancellationToken { get; set; }

    public DataGridIntent Snapshot() => new()
    {
        Skip = Skip, Take = Take, ReadPolicy = ReadPolicy, CancellationToken = CancellationToken,
        Filters = Filters.Select(CloneFilter).ToList(),
        Sorts = Sorts.Select(s => new AppSortDescriptor { Property = s.Property, Direction = s.Direction }).ToList()
    };

    private static AppFilterDescriptor CloneFilter(AppFilterDescriptor f) => new()
    {
        Property = f.Property, Value = f.Value is Array array ? array.Clone() : f.Value,
        LogicalOperator = f.LogicalOperator, ComparisonOperator = f.ComparisonOperator,
        FilterValueType = f.FilterValueType, Filters = f.Filters.Select(CloneFilter).ToList()
    };

    public List<AppFilterDescriptor> Filters { get; set; } = new();
    public List<AppSortDescriptor> Sorts { get; set; } = new();
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 10;
}
