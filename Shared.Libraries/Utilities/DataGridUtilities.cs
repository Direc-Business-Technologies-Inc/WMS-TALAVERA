using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Libraries.Utilities;

// build solely because i dont like having to do new AppFilterDescriptor() every time
// i could have gone with a ComparisonOperatorEnum extension but i didn't really like it
public static class DataGridFilterUtilities
{
    public static AppFilterDescriptor Filter(string property, ComparisonOperatorEnum op, object value)
    {
        return new AppFilterDescriptor()
        {
            Property = property,
            ComparisonOperator = op,
            Value = value
        };
    }

    public static AppFilterDescriptor Equal(string property, object value) => Filter(property, ComparisonOperatorEnum.Equals, value);
    public static AppFilterDescriptor NotEqual(string property, object value) => Filter(property, ComparisonOperatorEnum.NotEquals, value);
    public static AppFilterDescriptor GreaterThan(string property, object value) => Filter(property, ComparisonOperatorEnum.GreaterThan, value);
    public static AppFilterDescriptor GreaterThanOrEqual(string property, object value) => Filter(property, ComparisonOperatorEnum.GreaterThanOrEqual, value);
    public static AppFilterDescriptor LessThan(string property, object value) => Filter(property, ComparisonOperatorEnum.LessThan, value);
    public static AppFilterDescriptor LessThanOrEqual(string property, object value) => Filter(property, ComparisonOperatorEnum.LessThanOrEqual, value);
    public static AppFilterDescriptor In(string property, object value) => Filter(property, ComparisonOperatorEnum.In, value);
    public static AppFilterDescriptor NotIn(string property, object value) => Filter(property, ComparisonOperatorEnum.NotIn, value);
    public static AppFilterDescriptor Contains(string property, object value) => Filter(property, ComparisonOperatorEnum.Contains, value);
    public static AppFilterDescriptor StartsWith(string property, object value) => Filter(property, ComparisonOperatorEnum.StartsWith, value);
    public static AppFilterDescriptor EndsWith(string property, object value) => Filter(property, ComparisonOperatorEnum.EndsWith, value);

    public static AppFilterDescriptor CreateFilter(this ComparisonOperatorEnum op, string property, object value) => Filter(property, op, value);
    
    public static AppFilterDescriptor Group(LogicalOperatorEnum op, params AppFilterDescriptor[] filters)
    {
        return new AppFilterDescriptor()
        {
            LogicalOperator = op,
            Filters = [.. filters]
        };
    }

    public static AppFilterDescriptor All(params AppFilterDescriptor[] filters) => Group(LogicalOperatorEnum.AND, filters);
    public static AppFilterDescriptor Any(params AppFilterDescriptor[] filters) => Group(LogicalOperatorEnum.OR, filters);
}

public static class DataGridSortUtilities
{
    public static AppSortDescriptor Sort(SortDirectionEnum direction, string property)
    {
        return new AppSortDescriptor()
        {
            Property = property,
            Direction = direction
        };
    }

    public static AppSortDescriptor Descending(string property) => Sort(SortDirectionEnum.Descending, property);
    public static AppSortDescriptor Ascending(string property) => Sort(SortDirectionEnum.Ascending, property);
}

