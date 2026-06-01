using Shared.Entities;

namespace Web.BlazorServer.Services.Repositories;

public interface IDataGridDescriptorOperatorAdapter
{
    ComparisonOperatorEnum GetComparisonOperator(dynamic filterOperator);
    LogicalOperatorEnum GetLogicalOperator(dynamic logicalOperator);
    SortDirectionEnum GetSortDirection(dynamic sort);
}
