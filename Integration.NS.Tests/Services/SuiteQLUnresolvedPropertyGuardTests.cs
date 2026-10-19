using Integration.NS.Services;
using Shared.Entities;
using Shared.Libraries.Utilities;
using Xunit;

namespace Integration.NS.Tests.Services;

/// <summary>
/// Regression coverage for NetSuite SuiteQL property resolution (Rule-7).
///
/// Bug: a grid sort/filter whose property name resolved to nothing (a SELECT
/// alias with no PropertyMap entry, or a stale property name replayed from
/// persisted grid settings) was emitted verbatim as a bare identifier. NetSuite
/// resolves identifiers against table aliases only and rejected the query with
/// 400 INVALID_PARAMETER: "Unknown identifier 'DestinationSubsidiary'".
///
/// Invariants under test:
/// - PropertyMap entries always win.
/// - Grid-sourced properties matching a SELECT alias expand to the underlying
///   column expression (SELECT aliases are not resolvable by NetSuite).
/// - Grid-sourced properties that resolve to nothing are DROPPED, never emitted
///   raw — a stale sort/filter must not 400 the whole grid.
/// - Hand-written (WithFilters) bare columns keep the legacy passthrough, since
///   they are author-controlled SQL.
/// </summary>
public class SuiteQLUnresolvedPropertyGuardTests
{
    private static SuiteQLQueryBuilderFactoryService Factory => new();

    [Fact]
    public void Build_PropertyMapEntry_WinsOverAlias()
    {
        var query = Factory.Create()
            .Select(("t.trandate", "Date"))
            .From("transaction t")
            .WithDatagridIntent(new DataGridIntent
            {
                Take = 10,
                Sorts = { DataGridSortUtilities.Descending("Date") }
            }, new Dictionary<string, string> { ["Date"] = "t.lastmodifieddate" })
            .Build();

        Assert.Contains("ORDER BY t.lastmodifieddate DESC", query.Query);
    }

    [Fact]
    public void Build_GridSortOnSelectAlias_ExpandsToColumnExpression()
    {
        var query = Factory.Create()
            .Select(("BUILTIN.DF(t.tosubsidiary)", "DestinationSubsidiary"))
            .From("transaction t")
            .WithDatagridIntent(new DataGridIntent
            {
                Take = 10,
                Sorts = { DataGridSortUtilities.Ascending("DestinationSubsidiary") }
            })
            .Build();

        Assert.Contains("ORDER BY BUILTIN.DF(t.tosubsidiary) ASC", query.Query);
    }

    [Fact]
    public void Build_UnresolvedGridSort_IsDropped()
    {
        var query = Factory.Create()
            .Select(("t.tranid", "ReferenceNumber"))
            .From("transaction t")
            .WithDatagridIntent(new DataGridIntent
            {
                Take = 10,
                Sorts = { DataGridSortUtilities.Ascending("DestinationSubsidiary") }
            })
            .Build();

        Assert.DoesNotContain("ORDER BY", query.Query);
    }

    [Fact]
    public void Build_UnresolvedGridFilter_IsDroppedButOtherFiltersKept()
    {
        var query = Factory.Create()
            .Select(("t.tranid", "ReferenceNumber"))
            .From("transaction t")
            .WithDatagridIntent(new DataGridIntent
            {
                Take = 10,
                Filters =
                {
                    DataGridFilterUtilities.Equal("DestinationSubsidiary", "PH"),
                    DataGridFilterUtilities.Equal("t.recordtype", "transferorder"),
                }
            })
            .Build();

        Assert.Contains("t.recordtype = 'transferorder'", query.Query);
        Assert.DoesNotContain("DestinationSubsidiary", query.Query);
    }

    [Fact]
    public void Build_HandWrittenBareColumnFilter_StillEmitted()
    {
        var query = Factory.Create()
            .Select(("name", "Name"))
            .From("customrecord_example")
            .WithDatagridIntent(new DataGridIntent { Take = 10 })
            .WithFilters(
                DataGridFilterUtilities.Equal("custrecord_unmapped_column", "x"))
            .Build();

        Assert.Contains("WHERE custrecord_unmapped_column = 'x'", query.Query);
    }

    [Fact]
    public void Build_GridFilterGroupWithNoProperty_StillAppliesItsChildren()
    {
        // The ItemDataGrid search box builds an OR-group that carries only a
        // LogicalOperator and children (no Property of its own). Resolving the
        // group's own empty Property dropped the entire group, so the search
        // silently unfiltered and the grid kept showing the prior rows.
        var query = Factory.Create()
            .Select(("i.itemid", "ItemNumber"), ("i.displayname", "Name"))
            .From("item i")
            .WithDatagridIntent(new DataGridIntent
            {
                Take = 10,
                Filters =
                {
                    DataGridFilterUtilities.Any(
                        DataGridFilterUtilities.Contains("ItemNumber", "abc"),
                        DataGridFilterUtilities.Contains("Name", "abc"))
                }
            })
            .Build();

        Assert.Contains("LOWER(i.itemid) LIKE LOWER('%abc%')", query.Query);
        Assert.Contains("LOWER(i.displayname) LIKE LOWER('%abc%')", query.Query);
        Assert.Contains(" OR ", query.Query);
    }

    [Fact]
    public void Build_GridFilterGroup_DropsUnresolvedChildWithoutMalformedSql()
    {
        // A child that resolves to nothing must be dropped before the group is
        // joined; otherwise string.Join emits an empty segment and the predicate
        // becomes "(a OR )", which NetSuite rejects.
        var query = Factory.Create()
            .Select(("i.itemid", "ItemNumber"))
            .From("item i")
            .WithDatagridIntent(new DataGridIntent
            {
                Take = 10,
                Filters =
                {
                    DataGridFilterUtilities.Any(
                        DataGridFilterUtilities.Contains("ItemNumber", "abc"),
                        DataGridFilterUtilities.Contains("NotARealColumn", "x"))
                }
            })
            .Build();

        Assert.Contains("LOWER(i.itemid) LIKE LOWER('%abc%')", query.Query);
        Assert.DoesNotContain("( OR", query.Query);
        Assert.DoesNotContain("OR )", query.Query);
        Assert.DoesNotContain("()", query.Query);
    }
}
