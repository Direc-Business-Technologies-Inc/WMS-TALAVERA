using Shared.Entities;
using Shared.Libraries.Utilities;

namespace Web.BlazorServer.Components.Custom;

public static class ReceivingReceiptPrintStatusFilter
{
    public static void ApplyToTransactions(DataGridIntent intent, PrintStatus status, string source = "PO")
    {
        PrintStatusFilter.ApplyApproval(intent, status);
        intent.Filters.RemoveAll(filter => filter.Property is "ForPrintingIRCount" or "PrintedIRCount");
        // Completed sources can be listed for an existing report, but must not
        // enter the zero-receipt branch used for new receiving work.
        var openStatuses = source == "PO" ? new[] { "B", "D", "E" } : new[] { "E", "F" };
        if (status == PrintStatus.ForPrinting)
            intent.Filters.Add(DataGridFilterUtilities.Any(
                DataGridFilterUtilities.GreaterThan("ForPrintingIRCount", 0),
                DataGridFilterUtilities.All(
                    DataGridFilterUtilities.In("t.status", openStatuses),
                    DataGridFilterUtilities.Equal("ForPrintingIRCount", 0),
                    DataGridFilterUtilities.Equal("PrintedIRCount", 0))));
        else if (status == PrintStatus.Printed)
            intent.Filters.Add(DataGridFilterUtilities.GreaterThan("PrintedIRCount", 0));
    }

    public static void ApplyToReceipts(DataGridIntent intent, PrintStatus status, string source)
    {
        PrintStatusFilter.Apply(intent, status, "tcf.custbody_dbti_custom_approval_status");
        intent.Filters.Add(DataGridFilterUtilities.In("tcf.status",
            source == "PO" ? new[] { "B", "D", "E", "F", "G" } : new[] { "E", "F", "G" }));
        if (source == "PO")
            intent.Filters.Add(DataGridFilterUtilities.Equal("tcf.recordtype", "purchaseorder"));
        else
        {
            intent.Filters.Add(DataGridFilterUtilities.In("tcf.recordtype", new[] { "transferorder", "intercompanytransferorder" }));
            if (source == "RETURNS")
                intent.Filters.Add(DataGridFilterUtilities.Any(
                    DataGridFilterUtilities.Equal("tcf.custbody_dbti_transfer_category", 3),
                    DataGridFilterUtilities.Equal("tcf.custbody_dbti_transfer_category", 4)));
            else
            {
                intent.Filters.Add(DataGridFilterUtilities.NotEqual("tcf.custbody_dbti_transfer_category", 3));
                intent.Filters.Add(DataGridFilterUtilities.NotEqual("tcf.custbody_dbti_transfer_category", 4));
            }
        }
    }
}
