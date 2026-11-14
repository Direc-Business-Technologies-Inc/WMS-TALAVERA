using Shared.Entities;
using Shared.Libraries.Utilities;

namespace Web.BlazorServer.Components.Custom;

public static class TripTicketPrintStatusFilter
{
    public const string PrintCountProperty = "PrintCount";

    public static void Apply(DataGridIntent intent, PrintStatus status)
    {
        intent.Filters.RemoveAll(filter => filter.Property == PrintCountProperty ||
            filter.Filters?.Any(child => child.Property == PrintCountProperty) == true);
        if (status == PrintStatus.ForPrinting)
            intent.Filters.Add(DataGridFilterUtilities.Any(
                DataGridFilterUtilities.IsNull(PrintCountProperty),
                DataGridFilterUtilities.LessThanOrEqual(PrintCountProperty, 0)));
        else if (status == PrintStatus.Printed)
            intent.Filters.Add(DataGridFilterUtilities.GreaterThanOrEqual(PrintCountProperty, 1));
    }
}
