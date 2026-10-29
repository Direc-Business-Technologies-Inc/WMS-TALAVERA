using Shared.Entities;
using Shared.Libraries.Utilities;

namespace Web.BlazorServer.Components.Custom;

public enum PrintStatus
{
    ForPrinting,
    Printed,
    All
}

public static class PrintStatusFilter
{
    // All callers list NetSuite transactions under the alias t. Trip Tickets are
    // custom records and must not use this filter until their field is confirmed.
    private const string PrintCountColumn = "t.custbody_wms_print_count";

    public static void Apply(DataGridIntent intent, PrintStatus status)
    {
        intent.Filters.RemoveAll(filter => filter.Property == PrintCountColumn);

        if (status == PrintStatus.ForPrinting)
            intent.Filters.Add(DataGridFilterUtilities.IsNull(PrintCountColumn));
        else if (status == PrintStatus.Printed)
            intent.Filters.Add(DataGridFilterUtilities.GreaterThanOrEqual(PrintCountColumn, 1));
    }

    public static void ApplyDateRange(DataGridIntent intent, DateTime? start, DateTime? end, string property = "Date")
    {
        if (start.HasValue)
            intent.Filters.Add(DataGridFilterUtilities.GreaterThanOrEqual(property, start.Value.Date));
        if (end.HasValue)
            intent.Filters.Add(DataGridFilterUtilities.LessThan(property, end.Value.Date.AddDays(1)));
    }

    public static bool TryGetDateRange<T>(IEnumerable<T> rows, Func<T, DateTime?> date, out (DateTime Start, DateTime End) range)
    {
        var dates = rows.Select(date).Where(value => value.HasValue).Select(value => value!.Value.Date).ToArray();
        if (dates.Length == 0)
        {
            range = default;
            return false;
        }

        range = (dates.Min(), dates.Max());
        return true;
    }

    public static async Task<int> CountAsync<T>(
        PrintStatus status,
        Func<DataGridIntent, Task<(IEnumerable<T>, int)>> getter)
    {
        var intent = new DataGridIntent { Skip = 0, Take = 1 };
        Apply(intent, status);
        var (_, count) = await getter(intent);
        return count;
    }
}
