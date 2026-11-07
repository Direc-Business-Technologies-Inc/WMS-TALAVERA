using Shared.Entities;
using Shared.Libraries.Utilities;

namespace Web.BlazorServer.Components.Custom;

public static class ReceivingReceiptPrintStatusFilter
{
    public static void ApplyToTransactions(DataGridIntent intent, PrintStatus status)
    {
        PrintStatusFilter.ApplyApproval(intent, status);
        intent.Filters.RemoveAll(filter => filter.Property is "ForPrintingIRCount" or "PrintedIRCount");
        if (status == PrintStatus.ForPrinting)
            intent.Filters.Add(DataGridFilterUtilities.Any(
                DataGridFilterUtilities.GreaterThan("ForPrintingIRCount", 0),
                DataGridFilterUtilities.All(
                    DataGridFilterUtilities.Equal("ForPrintingIRCount", 0),
                    DataGridFilterUtilities.Equal("PrintedIRCount", 0))));
        else if (status == PrintStatus.Printed)
            intent.Filters.Add(DataGridFilterUtilities.GreaterThan("PrintedIRCount", 0));
    }

    public static void ApplyToReceipts(DataGridIntent intent, PrintStatus status, string source)
    {
        PrintStatusFilter.Apply(intent, status, "tcf.custbody_dbti_custom_approval_status");
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
