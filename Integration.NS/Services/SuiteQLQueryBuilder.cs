using Application.DataTransferObjects.Others.NS;
using Shared.Entities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Integration.NS.Services;


public class SuiteQLQueryBuilderFactoryService
{
    public SuiteQLQueryBuilder Create(string baseQuery)
    {
        return new SuiteQLQueryBuilder { BaseQuery = baseQuery };
    }
}

public class SuiteQLQueryBuilder
{
    const string DATETIME_FORMAT_STRING = "YYYY-MM-DDTHH:mm:ss";

    public required string BaseQuery { get; set; }
    public int? Take { get; set; }
    public int? Skip { get; set; }
    private string? SortQuery { get; set; }
    private List<string> Filters { get; set; } = [];

    public SuiteQLQueryBuilder ApplyDataGridIntent(DataGridIntent intent, Dictionary<string, string>? mapFields = null)
    {
        ApplyDataGridFilters(intent, mapFields);
        ApplyDataGridSorts(intent, mapFields);
        Take = intent.Take;
        return this;
    }
    public SuiteQLQueryBuilder ApplyDataGridFilters(DataGridIntent intent, Dictionary<string, string>? mapFields = null)
    {
        try
        {
            if (intent.Filters.Count > 0)
                Filters.AddRange(intent.Filters.Select(f => _parseFilter(f, mapFields)));
        }
        catch (Exception ex) {
            throw new Exception($"Caught {ex.GetType().Name} while applying datagrid filters: {ex.Message}");
        }
        return this;
    }

    public SuiteQLQueryBuilder ApplyDataGridSorts(DataGridIntent intent, Dictionary<string, string>? mapFields = null)
    {
        try
        {
            if (intent.Sorts.Count > 0)
                SortQuery = " ORDER BY " + string.Join(", ", intent.Sorts.Select(s => _parseSort(s, mapFields)));
        }
        catch (Exception ex) {
            throw new Exception($"Caught {ex.GetType().Name} while applying datagrid sorts: {ex.Message}", ex);
        }
        return this;
    }

    public SuiteQLQuery Build()
    {
        var FilterQuery = Filters.Count > 0 ?
            " WHERE " + string.Join(" AND ", Filters) : string.Empty;

        return new()
        {
            Query = BaseQuery + FilterQuery + SortQuery,
            Limit = Take,
            Offset = Skip
        };
    }

    private string _parseSort(AppSortDescriptor sort, Dictionary<string, string>? mapFields = null)
    {
        var sortDir = sort.Direction switch
        {
            SortDirectionEnum.Descending => "DESC",
            SortDirectionEnum.Ascending => "ASC",
            _ => throw new ArgumentException("Invalid sort direction")
        };
        var property = mapFields != null && mapFields.ContainsKey(sort.Property) ? mapFields[sort.Property] : sort.Property;
        return $"{property} {sortDir}";
    }

    private string _parseFilter(AppFilterDescriptor filter, Dictionary<string, string>? mapFields = null)
    {
        if (filter.Filters.Count > 0) return _parseFilterGroup(filter);
        if (filter.ComparisonOperator == null) throw new InvalidOperationException($"no comparison operator given");
        switch (filter.ComparisonOperator)
        {
            case ComparisonOperatorEnum.Equals:
                return _binary(filter.Property, "=", filter.Value, mapFields);
            case ComparisonOperatorEnum.NotEquals :
                return _binary(filter.Property, "!=", filter.Value, mapFields);
            case ComparisonOperatorEnum.GreaterThan  :
                return _binary(filter.Property, ">", filter.Value, mapFields);
            case ComparisonOperatorEnum.GreaterThanOrEqual  :
                return _binary(filter.Property, ">=", filter.Value, mapFields);
            case ComparisonOperatorEnum.LessThan :
                return _binary(filter.Property, "<", filter.Value, mapFields);
            case ComparisonOperatorEnum.LessThanOrEqual  :
                return _binary(filter.Property, "<=", filter.Value, mapFields);
            case ComparisonOperatorEnum.Contains :
                if (filter.Value is not string) throw new InvalidOperationException($"{filter.Property} is not a string and does not support the contains operation");
                return _binary(filter.Property, "LIKE", $"%{filter.Value}%", mapFields);
            case ComparisonOperatorEnum.StartsWith:
                if (filter.Value is not string) throw new InvalidOperationException($"{filter.Property} is not a string and does not support the starts with operation");
                return _binary(filter.Property, "LIKE", $"{filter.Value}%", mapFields);
            case ComparisonOperatorEnum.EndsWith:
                if (filter.Value is not string) throw new InvalidOperationException($"{filter.Property} is not a string and does not support the ends with operation");
                return _binary(filter.Property, "LIKE", $"%{filter.Value}", mapFields);
            case ComparisonOperatorEnum.IsEmpty  :
            case ComparisonOperatorEnum.IsNotNull :
            case ComparisonOperatorEnum.IsNotEmpty:
            case ComparisonOperatorEnum.In :
            case ComparisonOperatorEnum.NotIn :
            case ComparisonOperatorEnum.IsNull :
                break;
        }

        throw new NotImplementedException($"Error at {nameof(SuiteQLQueryBuilder)}: {filter.ComparisonOperator} is not implemented ");
    }

    public SuiteQLQueryBuilder AddFilter(AppFilterDescriptor filter, Dictionary<string, string>? propertyMap = null)
    {
        Filters.Add(_parseFilter(filter, propertyMap));
        return this;
    }

    private string _binary(string prop, string op, object? value, Dictionary<string, string>? mapFields = null)
    {
        if (value is null) throw new InvalidOperationException($"no value given for {prop}");
        if (value is DateTime) value = ((DateTime)value).ToString(DATETIME_FORMAT_STRING);

        if (value is string) value = $"'{value}'";
        else value = JsonSerializer.Serialize(value);

        prop = mapFields != null && mapFields.ContainsKey(prop) ? mapFields[prop] : prop;

        return $"{prop} {op} {value}";
        //return new QueryFilter { Query = $"{prop} {op} ?", Parameters = [value] };
    }

    private string _parseFilterGroup(AppFilterDescriptor filter, Dictionary<string, string>? mapFields = null)
    {
        if (filter.LogicalOperator is null) throw new InvalidOperationException("Filter group given but no logical operator");
        string op = filter.LogicalOperator switch
        {
            LogicalOperatorEnum.AND => "AND",
            LogicalOperatorEnum.OR => "OR",
            _ => " AND "
        };

        return string.Join($" {op} ", filter.Filters.Select(x => _parseFilter(x, mapFields)));
        //return new QueryFilterGroup
        //{
        //    LogicalOperator = op,
        //    Filters = filter.Filters.Select(f => _parseFilter(f, mapFields)).ToList()
        //};
    }



    private class QueryFilter
    {
        public virtual string Query { get; set; } = string.Empty;
        public virtual object[] Parameters { get; set; } = [];
    }

    private class QueryFilterGroup : QueryFilter
    {
        public string LogicalOperator { get; set; } = "AND";
        public List<QueryFilter> Filters { get; set; } = [];
        public override string Query => "(" + string.Join($" {LogicalOperator} ", Filters.Select(f => f.Query)) + ")";
        public override object[] Parameters => Filters.SelectMany(f => f.Parameters).ToArray();
    }
}