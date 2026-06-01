using Radzen;
using Shared.Entities;
using Web.BlazorServer.Services.Repositories;

namespace Web.BlazorServer.Services.Implementation;

public class DataGridDescriptorOperatorAdapter : IDataGridDescriptorOperatorAdapter
{
    public ComparisonOperatorEnum GetComparisonOperator(dynamic filterOperator)
    {
        return filterOperator switch
        {
            FilterOperator.Equals => ComparisonOperatorEnum.Equals,
            FilterOperator.NotEquals => ComparisonOperatorEnum.NotEquals,
            FilterOperator.LessThan => ComparisonOperatorEnum.LessThan,
            FilterOperator.LessThanOrEquals => ComparisonOperatorEnum.LessThanOrEqual,
            FilterOperator.GreaterThan => ComparisonOperatorEnum.GreaterThan,
            FilterOperator.GreaterThanOrEquals => ComparisonOperatorEnum.GreaterThanOrEqual,
            FilterOperator.Contains => ComparisonOperatorEnum.Contains,
            FilterOperator.StartsWith => ComparisonOperatorEnum.StartsWith,
            FilterOperator.EndsWith => ComparisonOperatorEnum.EndsWith,
            FilterOperator.IsEmpty => ComparisonOperatorEnum.IsEmpty,
            FilterOperator.IsNotEmpty => ComparisonOperatorEnum.IsNotEmpty,
            FilterOperator.IsNull => ComparisonOperatorEnum.IsNull,
            FilterOperator.IsNotNull => ComparisonOperatorEnum.IsNotNull,
            _ => throw new NotSupportedException($"Filter operator '{filterOperator}' is not supported.")
        };
    }

    public LogicalOperatorEnum GetLogicalOperator(dynamic logicalOperator)
    {
        return logicalOperator switch
        {
            LogicalFilterOperator.Or => LogicalOperatorEnum.OR,
            LogicalFilterOperator.And => LogicalOperatorEnum.AND,
            _ => throw new NotSupportedException($"Logical operator '{logicalOperator}' is not supported")
        };
    }

    public SortDirectionEnum GetSortDirection(dynamic sort)
    {
        return sort switch
        {
            SortOrder.Ascending => SortDirectionEnum.Ascending,
            SortOrder.Descending => SortDirectionEnum.Descending,
            _ => throw new NotSupportedException($"Sort direction '{sort}' is not supported")
        };
    }
}
