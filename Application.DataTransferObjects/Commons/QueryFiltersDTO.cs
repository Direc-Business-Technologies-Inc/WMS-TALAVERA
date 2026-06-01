using Shared.Entities;
using Shared.Kernel;

namespace Application.DataTransferObjects.Commons;

public class QueryFiltersDTO
{
    public int Offset { get; set; } = 0;
    public int Fetch { get; set; } = 10;
    public string? Property { get; set; } = null;
    public string? Direction { get; set; } = null;

    public static QueryFiltersDTO CreateFromIntent(DataGridIntent intent)
    {
        var dto = new QueryFiltersDTO
        {
            Offset = intent.Skip,
            Fetch = intent.Take <= 0 ? 20 : intent.Take,
            Property = intent.Sorts.FirstOrDefault()?.Property ?? null,
            Direction = EnumHelper.GetEnumDescription(intent.Sorts.FirstOrDefault()?.Direction ?? SortDirectionEnum.Descending)
        };

        return dto;
    }
}
