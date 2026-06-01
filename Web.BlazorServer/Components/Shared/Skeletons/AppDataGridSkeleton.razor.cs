using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace Web.BlazorServer.Components.Shared.Skeletons;

public partial class AppDataGridSkeleton<TItem>
{
    [Parameter] public bool Loading { get; set; } = true;

    public int Columns<T>()
    {
        Type type = typeof(T);
        var properties = type.GetProperties();
        return properties.Length;
    }
}
