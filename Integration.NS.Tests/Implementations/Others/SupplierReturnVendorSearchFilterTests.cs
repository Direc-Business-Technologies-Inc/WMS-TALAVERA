using Shared.Entities;
using Shared.Libraries.Utilities;
using Xunit;

namespace Integration.NS.Tests.Implementations.Others;

public class SupplierReturnVendorSearchFilterTests
{
    private const string VendorSearchProperty = "ReferenceNumber";

    [Fact]
    public void Search_filter_matches_vendor_reference_or_name()
    {
        const string search = "ACME";
        var intent = new DataGridIntent
        {
            Filters = [DataGridFilterUtilities.Contains(VendorSearchProperty, search)]
        };

        var searchFilter = intent.Filters.First(x =>
            x.Filters.Count == 0 &&
            x.Property == VendorSearchProperty &&
            x.Value is string value &&
            !string.IsNullOrWhiteSpace(value));

        intent.Filters.Remove(searchFilter);
        intent.Filters.Add(DataGridFilterUtilities.Any(
            DataGridFilterUtilities.Contains(VendorSearchProperty, search),
            DataGridFilterUtilities.Contains("Name", search)));

        var group = Assert.Single(intent.Filters);
        Assert.Equal(LogicalOperatorEnum.OR, group.LogicalOperator);
        Assert.Collection(group.Filters,
            filter =>
            {
                Assert.Equal("ReferenceNumber", filter.Property);
                Assert.Equal(ComparisonOperatorEnum.Contains, filter.ComparisonOperator);
                Assert.Equal(search, filter.Value);
            },
            filter =>
            {
                Assert.Equal("Name", filter.Property);
                Assert.Equal(ComparisonOperatorEnum.Contains, filter.ComparisonOperator);
                Assert.Equal(search, filter.Value);
            });
    }

    [Fact]
    public void Search_filter_translation_preserves_other_intent_filters()
    {
        var subsidiaryFilter = DataGridFilterUtilities.Equal("SubsidiaryId", 7);
        var searchFilter = DataGridFilterUtilities.Contains(VendorSearchProperty, "ACME");
        var intent = new DataGridIntent { Filters = [subsidiaryFilter, searchFilter] };

        intent.Filters.Remove(searchFilter);
        intent.Filters.Add(DataGridFilterUtilities.Any(
            DataGridFilterUtilities.Contains(VendorSearchProperty, "ACME"),
            DataGridFilterUtilities.Contains("Name", "ACME")));

        Assert.Contains(subsidiaryFilter, intent.Filters);
        Assert.Equal(2, intent.Filters.Count);
        Assert.Contains(intent.Filters, filter => filter.LogicalOperator == LogicalOperatorEnum.OR);
    }
}
