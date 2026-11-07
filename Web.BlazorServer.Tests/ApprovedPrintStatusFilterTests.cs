using Integration.NS.Services;
using Shared.Entities;
using Web.BlazorServer.Components.Custom;
using Xunit;

namespace Web.BlazorServer.Tests;

public class ApprovedPrintStatusFilterTests
{
    [Theory]
    [InlineData("Transaction", PrintStatus.ForPrinting)]
    [InlineData("Transaction", PrintStatus.Printed)]
    [InlineData("Transaction", PrintStatus.All)]
    [InlineData("Packing", PrintStatus.ForPrinting)]
    [InlineData("Packing", PrintStatus.Printed)]
    [InlineData("Packing", PrintStatus.All)]
    [InlineData("Receiving", PrintStatus.ForPrinting)]
    [InlineData("Receiving", PrintStatus.Printed)]
    [InlineData("Receiving", PrintStatus.All)]
    public void Parent_queries_require_approved_only_for_printing(string kind, PrintStatus status)
    {
        var intent = new DataGridIntent();
        ApplyParent(intent, kind, status);
        var query = Query(intent);

        if (status == PrintStatus.ForPrinting)
        {
            Assert.Contains("t.custbody_dbti_custom_approval_status = 4", query);
            // Approval must be a separate AND gate, including the no-child branch.
            Assert.Contains(intent.Filters, f => f.Property == "t.custbody_dbti_custom_approval_status" && f.Filters.Count == 0);
            Assert.Equal(2, intent.Filters.Count);
        }
        else
            Assert.DoesNotContain("custbody_dbti_custom_approval_status", query);
    }

    [Theory]
    [InlineData("STR", true)]
    [InlineData("RETURNS", true)]
    [InlineData("VRA", true)]
    [InlineData("PO", false)]
    [InlineData("STR", false)]
    [InlineData("RETURNS", false)]
    public void Child_queries_check_parent_approval_and_preserve_printed_and_all_views(string source, bool packing)
    {
        foreach (var status in new[] { PrintStatus.ForPrinting, PrintStatus.Printed, PrintStatus.All })
        {
            var intent = new DataGridIntent();
            if (packing)
                PackingFulfillmentPrintStatusFilter.ApplyToFulfillments(intent, status, source, 7);
            else
                ReceivingReceiptPrintStatusFilter.ApplyToReceipts(intent, status, source);
            var query = Query(intent);

            Assert.DoesNotContain("t.custbody_dbti_custom_approval_status", query);
            if (status == PrintStatus.ForPrinting)
            {
                Assert.Contains("tcf.custbody_dbti_custom_approval_status = 4", query);
                Assert.Matches(@"t\.custbody_wms_print_count IS NULL\s+OR t\.custbody_wms_print_count <= 0", query);
            }
            else
            {
                Assert.DoesNotContain("custbody_dbti_custom_approval_status", query);
                if (status == PrintStatus.Printed)
                    Assert.Contains("t.custbody_wms_print_count >= 1", query);
            }
        }
    }

    [Theory]
    [InlineData("Transaction")]
    [InlineData("Packing")]
    [InlineData("Receiving")]
    public void Reusing_an_intent_does_not_leave_approval_restriction_on_other_tabs(string kind)
    {
        var intent = new DataGridIntent();
        ApplyParent(intent, kind, PrintStatus.ForPrinting);
        ApplyParent(intent, kind, PrintStatus.ForPrinting);
        Assert.Single(intent.Filters, f => f.Property == "t.custbody_dbti_custom_approval_status");

        ApplyParent(intent, kind, PrintStatus.Printed);
        Assert.DoesNotContain("custbody_dbti_custom_approval_status", Query(intent));
        ApplyParent(intent, kind, PrintStatus.All);
        Assert.DoesNotContain("custbody_dbti_custom_approval_status", Query(intent));
    }

    [Theory]
    [InlineData(PrintStatus.ForPrinting)]
    [InlineData(PrintStatus.Printed)]
    public async Task Transaction_badges_use_the_same_approval_gate_as_rows(PrintStatus status)
    {
        string? countQuery = null;
        await PrintStatusFilter.CountAsync<object>(status, intent =>
        {
            countQuery = Query(intent);
            return Task.FromResult<(IEnumerable<object>, int)>(([], 0));
        });
        var rowIntent = new DataGridIntent();
        PrintStatusFilter.Apply(rowIntent, status);
        Assert.Equal(Query(rowIntent), countQuery);
    }

    private static void ApplyParent(DataGridIntent intent, string kind, PrintStatus status)
    {
        if (kind == "Packing") PackingFulfillmentPrintStatusFilter.Apply(intent, status);
        else if (kind == "Receiving") ReceivingReceiptPrintStatusFilter.ApplyToTransactions(intent, status);
        else PrintStatusFilter.Apply(intent, status);
    }

    private static string Query(DataGridIntent intent) => new SuiteQLQueryBuilder()
        .From("transaction t")
        .WithDatagridIntent(intent, new()
        {
            ["ForPrintingIFCount"] = "ifprint.forprintingcount",
            ["PrintedIFCount"] = "ifprint.printedcount",
            ["PendingPackLinesCount"] = "openpk.packtran",
            ["ForPrintingIRCount"] = "ircount.forprintingcount",
            ["PrintedIRCount"] = "ircount.printedcount"
        })
        .Build().Query;
}
