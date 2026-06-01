using Radzen;
using Shared.Entities;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.Services.Repositories;

namespace Web.BlazorServer.Services.Implementation;

public class DataGridIntentAdapter : IDataGridIntentAdapter
{
    LoadDataArgs _args { get; set; }

    public DataGridIntent QueryIntent { get; private set; } = new();
    public IDataGridDescriptorOperatorAdapter OperatorAdapter { get; set; } = new DataGridDescriptorOperatorAdapter();
    public DataGridIntentAdapter(LoadDataArgs? args)
    {
        _args = args ?? new LoadDataArgs();
    }
    public DataGridIntentAdapter(LoadDataArgs? args, DataGridIntent intent)
    {
        QueryIntent = intent;
        _args = args ?? new LoadDataArgs();
    }

    public void AdaptToMyFilterQueries()
    {
        if (_args.Filters is null)
            QueryIntent.Filters = [];
        else
            foreach (var filter in _args.Filters)
            {
                //property filter, it encloses the two 
                var filterQuery = new AppFilterDescriptor
                {
                    Property = string.IsNullOrEmpty(filter.FilterProperty) ? filter.Property : filter.FilterProperty,
                    LogicalOperator = OperatorAdapter.GetLogicalOperator(filter.LogicalFilterOperator),
                    FilterValueType = FilterValueTypeHelper.GetFilterValueTypeFromPropertyType(filter.Type ?? typeof(int)),
                };
                //filter for the first condition
                if (filter.FilterValue is not null)
                {
                    filterQuery.Filters.Add(new AppFilterDescriptor
                    {
                        Value = filter.FilterValue,
                        Property = string.IsNullOrEmpty(filter.FilterProperty) ? filter.Property : filter.FilterProperty,
                        FilterValueType = FilterValueTypeHelper.GetFilterValueTypeFromPropertyType(filter.Type ?? typeof(int)),
                        ComparisonOperator = OperatorAdapter.GetComparisonOperator(filter.FilterOperator)
                    });
                }
                else if (filter.FilterOperator == FilterOperator.IsNull || filter.FilterOperator == FilterOperator.IsNotNull)
                {
                    filterQuery.Filters.Add(new AppFilterDescriptor
                    {
                        Value = filter.FilterValue,
                        Property = string.IsNullOrEmpty(filter.FilterProperty) ? filter.Property : filter.FilterProperty,
                        FilterValueType = FilterValueTypeHelper.GetFilterValueTypeFromPropertyType(filter.Type ?? typeof(int)),
                        ComparisonOperator = OperatorAdapter.GetComparisonOperator(filter.FilterOperator)
                    });
                }
                //filter for the second condition
                if (filter.SecondFilterValue is not null)
                {
                    filterQuery.Filters.Add(new AppFilterDescriptor
                    {
                        Value = filter.SecondFilterValue,
                        Property = string.IsNullOrEmpty(filter.FilterProperty) ? filter.Property : filter.FilterProperty,
                        FilterValueType = FilterValueTypeHelper.GetFilterValueTypeFromPropertyType(filter.Type ?? typeof(int)),
                        ComparisonOperator = OperatorAdapter.GetComparisonOperator(filter.SecondFilterOperator)
                    });
                }
                QueryIntent.Filters.Add(filterQuery);
            }
    }

    public void AdaptToMySortQueries()
    {
        if (_args.Sorts is null)
            QueryIntent.Sorts = [];
        else
            foreach (var sort in _args.Sorts)
            {
                QueryIntent.Sorts.Add(new AppSortDescriptor
                {
                    Property = sort.Property,
                    Direction = OperatorAdapter.GetSortDirection(sort.SortOrder ?? SortOrder.Ascending)
                });
            }
    }
    public void AdaptToPagination()
    {
        if (_args.Top is null)
            QueryIntent.Take = 10;
        else
            QueryIntent.Take = _args.Top ?? 0;

        if (_args.Skip is null)
            QueryIntent.Skip = 0;
        else
            QueryIntent.Skip = _args.Skip ?? 0;
    }

    public void ClearIntent()
    {
        QueryIntent = new();
    }

    public void AddFilters(List<AppFilterDescriptor> filters) => QueryIntent.Filters.AddRange(filters);

    public void AddFilter(AppFilterDescriptor filter) => QueryIntent.Filters.Add(filter);

    public void RemoveFilter(AppFilterDescriptor filter) => 
        QueryIntent.Filters.RemoveAll(f => f.Property.Equals(filter.Property)  && f.Value is not null ? f.Property.Equals(filter.Value) : false);

    public void RemoveFilters(List<AppFilterDescriptor> filters) => QueryIntent.Filters.RemoveAll(f => filters.Any(fl => fl.Property.Equals(f.Property) && fl.Value is not null ? fl.Value.Equals(f.Value) : false));

    public void ClearFilters() => QueryIntent.Filters.Clear();
}
