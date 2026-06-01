namespace Web.BlazorServer.ViewModels.Abstraction;

public class DataGridResultVM<T> where T : class
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int Count { get; set; }

    public static DataGridResultVM<T> New(IEnumerable<T> items, int count)
    {
        return new DataGridResultVM<T>
        {
            Items = items,
            Count = count
        };
    }
}
