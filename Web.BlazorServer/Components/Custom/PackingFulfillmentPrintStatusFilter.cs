using Shared.Entities;
using Shared.Libraries.Utilities;

namespace Web.BlazorServer.Components.Custom;

public static class PackingFulfillmentPrintStatusFilter
{
    public static void Apply(DataGridIntent intent, PrintStatus status, string source = "STR")
    {
        PrintStatusFilter.ApplyApproval(intent, status);
        intent.Filters.RemoveAll(filter => filter.Property is "ForPrintingIFCount" or "PrintedIFCount");
        var openStatuses = source == "VRA" ? new[] { "B", "E" } : new[] { "B", "D", "E" };
        if (status == PrintStatus.ForPrinting)
            intent.Filters.Add(DataGridFilterUtilities.Any(
                DataGridFilterUtilities.GreaterThan("ForPrintingIFCount", 0),
                DataGridFilterUtilities.All(
                    DataGridFilterUtilities.In("t.status", openStatuses),
                    DataGridFilterUtilities.Equal("ForPrintingIFCount", 0),
                    DataGridFilterUtilities.Equal("PrintedIFCount", 0),
                    DataGridFilterUtilities.GreaterThan("PendingPackLinesCount", 0))));
        else if (status == PrintStatus.Printed)
            intent.Filters.Add(DataGridFilterUtilities.GreaterThan("PrintedIFCount", 0));
    }

    public static void ApplyToFulfillments(DataGridIntent intent, PrintStatus status, string source, int subsidiaryId)
    {
        PrintStatusFilter.Apply(intent, status, "tcf.custbody_dbti_custom_approval_status");
        intent.Filters.Add(DataGridFilterUtilities.Equal("tcf.subsidiary", subsidiaryId));
        if (source == "VRA")
        {
            intent.Filters.Add(DataGridFilterUtilities.Equal("tcf.recordtype", "vendorreturnauthorization"));
            intent.Filters.Add(DataGridFilterUtilities.In("tcf.status", new[] { "B", "E", "F", "G" }));
            return;
        }

        intent.Filters.Add(DataGridFilterUtilities.In("tcf.recordtype", new[] { "transferorder", "intercompanytransferorder" }));
        intent.Filters.Add(DataGridFilterUtilities.In("tcf.status", new[] { "B", "D", "E", "F", "G" }));
        intent.Filters.Add(DataGridFilterUtilities.In("tcf.custbody_dbti_transfer_category",
            source == "RETURNS" ? new[] { "3", "4" } : new[] { "1", "2" }));
    }
}
