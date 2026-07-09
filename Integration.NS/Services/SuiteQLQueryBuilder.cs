using Application.DataTransferObjects.Others.NS;
using Mapster;
using Microsoft.Net.Http.Headers;
using Shared.Entities;
using System.Collections;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Net.WebRequestMethods;

namespace Integration.NS.Services;


public class SuiteQLQueryBuilderFactoryService
{
    public SuiteQLQueryBuilder Create()
    {
        return new SuiteQLQueryBuilder();
    }
}

public class SuiteQLQueryBuilder
{
    const string DATETIME_FORMAT_STRING = "yyyy-MM-ddTHH:mm:ss";
    const string NETSUITE_DATETIME_FORMAT_STRING = "YYYY-MM-DD\"T\"HH24:MI:SS";
    public int? Take { get; set; }
    public int? Skip { get; set; }
    public List<AppFilterDescriptor> Filters { get; set; } = [];
    public List<AppSortDescriptor> Sorts { get; set; } = [];
    public List<(string col, string? alias)> SelectColumns { get; set; } = [];
    public Dictionary<string, string> PropertyMap { get; set; } = new();

    private string? _tableName;
    private List<string> _joins = [];
    private HashSet<string> _uniqueColumns = new();

    public SuiteQLQueryBuilder() { }
    public SuiteQLQueryBuilder(SuiteQLQueryBuilder source)
    {
        // TODO memory can be saved by usig a reference to the parent builder, but i doubt the query builder will take up too much memory so this is alright for now
        // The code is way too patchwork already to do more stuff, do it later when refactoring
        source.Filters.Adapt(Filters);
        source.Sorts.Adapt(Sorts);
        source.SelectColumns.Adapt(SelectColumns);
        source._joins.Adapt(_joins);
        PropertyMap = new Dictionary<string, string>(source.PropertyMap);

        Take = source.Take;
        Skip = source.Skip;

        _tableName = source._tableName;
    }

    public SuiteQLQueryBuilder WithDatagridIntent(DataGridIntent intent, Dictionary<string, string>? mapFields = null)
    {
        WithDataGridFilters(intent, mapFields);
        WithDataGridSorts(intent, mapFields);
        Take = intent.Take;
        Skip = intent.Skip;
        return this;
    }
    public SuiteQLQueryBuilder WithDataGridFilters(DataGridIntent intent, Dictionary<string, string>? mapFields = null)
    {
        Filters.AddRange(intent.Filters);
        return this;
    }

    public SuiteQLQueryBuilder WithDataGridSorts(DataGridIntent intent, Dictionary<string, string>? mapFields = null)
    {
        Sorts.AddRange(intent.Sorts);
        return this;
    }

    private string BuildFilters()
    {
        if (Filters.Count == 0) return string.Empty;

        try
        {
            return " WHERE " + string.Join(" AND ", Filters.Select(f => _parseFilter(f)));
        }
        catch (Exception ex)
        {
            throw new Exception($"Caught {ex.GetType().Name} while building filters: {ex.Message}", ex);
        }
    }
    private string BuildSorts()
    {
        if (Sorts.Count == 0) return string.Empty;

        try
        {
            return " ORDER BY " + string.Join(", ", Sorts.Select(s => _parseSort(s)));
        }
        catch (Exception ex)
        {
            throw new Exception($"Caught {ex.GetType().Name} while applying datagrid sorts: {ex.Message}", ex);
        }
    }

    private string BuildSelect()
    {
        if (SelectColumns.Count == 0) return "SELECT * ";
        return "SELECT " + string.Join(", ", SelectColumns.Select(c => c.alias != null ? $"{c.col} AS {c.alias}" : c.col));
    }

    private string BuildFrom()
    {
        if (string.IsNullOrEmpty(_tableName)) throw new InvalidOperationException("Table must be set");
        return $" FROM {_tableName} {string.Join(" ", _joins)}";
    }

    public SuiteQLQuery Build()
    {

        return new()
        {
            Query = BuildSelect() + BuildFrom() + BuildFilters() + BuildSorts(),
            Limit = Take,
            Offset = Skip
        };
    }

    private string _parseSort(AppSortDescriptor sort, Dictionary<string, string>? propertyMap = null)
    {
        propertyMap ??= PropertyMap;

        var sortDir = sort.Direction switch
        {
            SortDirectionEnum.Descending => "DESC",
            SortDirectionEnum.Ascending => "ASC",
            _ => throw new ArgumentException("Invalid sort direction")
        };
        var property = propertyMap != null && propertyMap.ContainsKey(sort.Property) ? propertyMap[sort.Property] : sort.Property;
        return $"{property} {sortDir}";
    }

    private string _parseFilter(AppFilterDescriptor filter, Dictionary<string, string>? propertyMap = null)
    {
        propertyMap ??= PropertyMap;

        if (filter.Filters.Count > 0) return _parseFilterGroup(filter);
        if (filter.ComparisonOperator == null) throw new InvalidOperationException($"no comparison operator given");
        switch (filter.ComparisonOperator)
        {
            case ComparisonOperatorEnum.Equals:
                return _binary(filter.Property, "=", filter.Value, propertyMap);
            case ComparisonOperatorEnum.NotEquals :
                return _binary(filter.Property, "!=", filter.Value, propertyMap);
            case ComparisonOperatorEnum.GreaterThan  :
                return _binary(filter.Property, ">", filter.Value, propertyMap);
            case ComparisonOperatorEnum.GreaterThanOrEqual  :
                return _binary(filter.Property, ">=", filter.Value, propertyMap);
            case ComparisonOperatorEnum.LessThan :
                return _binary(filter.Property, "<", filter.Value, propertyMap);
            case ComparisonOperatorEnum.LessThanOrEqual  :
                return _binary(filter.Property, "<=", filter.Value, propertyMap);
            case ComparisonOperatorEnum.Contains:
                return _stringOp(filter.Property, ComparisonOperatorEnum.Contains, filter.Value, propertyMap);
            case ComparisonOperatorEnum.StartsWith:
                return _stringOp(filter.Property, ComparisonOperatorEnum.StartsWith, filter.Value, propertyMap);
            case ComparisonOperatorEnum.EndsWith:
                return _stringOp(filter.Property, ComparisonOperatorEnum.EndsWith, filter.Value, propertyMap);
            case ComparisonOperatorEnum.In :
                return _listOp(filter.Property, "IN", filter.Value, propertyMap);
            case ComparisonOperatorEnum.NotIn :
                return _listOp(filter.Property, "NOT IN", filter.Value, propertyMap);
            case ComparisonOperatorEnum.IsEmpty  :
            case ComparisonOperatorEnum.IsNotNull :
            case ComparisonOperatorEnum.IsNotEmpty:
            case ComparisonOperatorEnum.IsNull :
                break;
        }

        throw new NotImplementedException($"Error at {nameof(SuiteQLQueryBuilder)}: {filter.ComparisonOperator} is not implemented ");
    }

    private string _stringifyValue(object value)
    {
        if (value is string strVal) return $"'{strVal}'";
        if (value is Literal literalVal) return literalVal.Value;
        if (value is DateTime dateVal) return $"'{dateVal.ToString(DATETIME_FORMAT_STRING)}'";
        if (value is null) return "NULL";
        if (value is object[] arrayVal) return "(" + string.Join(", ", arrayVal.Select(_stringifyValue)) + ")";
        if (value is IEnumerable enumerable)
        {
            bool firstItem = true;
            StringBuilder builder = new();
            builder.Append("(");
            foreach (var item in enumerable) { 
                if (!firstItem)
                {
                    builder.Append(',');
                }
                builder.Append(_stringifyValue(item));
                firstItem = false;
            }
            builder.Append(")");
            return builder.ToString();
        }
        return JsonSerializer.Serialize(value);
    }

    private string _listOp(string prop, string op, object? value, Dictionary<string, string>? propertyMap = null)
    {
        if (value is null || value is not IEnumerable) throw new InvalidOperationException($"operation {op} requires an array type as its value");

        prop = propertyMap != null && propertyMap.ContainsKey(prop) ? propertyMap[prop] : prop;

        return $"{prop} {op} {_stringifyValue(value)}";
    }

    public SuiteQLQueryBuilder WithFilter(AppFilterDescriptor filter, Dictionary<string, string>? propertyMap = null)
    {
        propertyMap ??= PropertyMap;

        Filters.Add(filter);

        return this;
    }

    public SuiteQLQueryBuilder WithFilters(params AppFilterDescriptor[] filters)
    {
        return WithFilters(null, filters);
    }

    public SuiteQLQueryBuilder WithFilters(Dictionary<string, string>? propertyMap, params AppFilterDescriptor[] filters)
    {
        foreach (var filter in filters)
        {
            WithFilter(filter, propertyMap);
        }

        return this;
    }

    private string _binary(string prop, string op, object? value, Dictionary<string, string>? propertyMap = null)
    {
        if (value is null) throw new InvalidOperationException($"no value given for {prop}");

        prop = propertyMap != null && propertyMap.ContainsKey(prop) ? propertyMap[prop] : prop;
        if (value is DateTime dtVal) return $"TO_DATE({prop}, '{NETSUITE_DATETIME_FORMAT_STRING}') {op} TO_DATE({_stringifyValue(dtVal)}, '{NETSUITE_DATETIME_FORMAT_STRING}')";
        return $"{prop} {op} {_stringifyValue(value)}";
    }

    private string _stringOp(string prop, ComparisonOperatorEnum op, object? value, Dictionary<string, string>? propertyMap = null)
    {
        if (value is null) throw new InvalidOperationException($"no value given for {prop}");
        if (value is not string strVal) throw new InvalidOperationException($"'{value}' is not a string and does not support {op}");

        prop = propertyMap != null && propertyMap.ContainsKey(prop) ? propertyMap[prop] : prop;

        return op switch
        {
            ComparisonOperatorEnum.Contains => $"LOWER({prop}) LIKE LOWER('%{strVal}%')",
            ComparisonOperatorEnum.EndsWith => $"LOWER({prop}) LIKE LOWER('%{strVal}')",
            ComparisonOperatorEnum.StartsWith => $"LOWER({prop}) LIKE LOWER('{strVal}%')",
            _ => throw new NotImplementedException($"string operation {op} is not implemented in this version of wms")
        };
    }

    private string _parseFilterGroup(AppFilterDescriptor filter, Dictionary<string, string>? propertyMap = null)
    {
        if (filter.LogicalOperator is null) throw new InvalidOperationException("Filter group given but no logical operator");
        string op = filter.LogicalOperator switch
        {
            LogicalOperatorEnum.AND => "AND",
            LogicalOperatorEnum.OR => "OR",
            _ => " AND "
        };

        return "(" + string.Join($" {op} ", filter.Filters.Select(x => _parseFilter(x, propertyMap))) + ")";
    }

    public SuiteQLQueryBuilder Select(params (string col, string? alias)[] columns)
    {
        foreach (var (col, alias) in columns)
        {
            var colTrimmed = col.Trim();
            var aliasTrimmed = alias?.Trim();
            var column = aliasTrimmed ?? colTrimmed;

            if (_uniqueColumns.Contains(column)) throw new InvalidOperationException($"Duplicate column selection: {column}");
            _uniqueColumns.Add(column);

            if (aliasTrimmed is not null) PropertyMap.TryAdd(aliasTrimmed, colTrimmed);

            SelectColumns.Add((colTrimmed, aliasTrimmed));
        }
        return this;
    }

    public SuiteQLQueryBuilder From(string table)
    {
        if (string.IsNullOrEmpty(table)) throw new ArgumentException("Table name cannot be null or empty", nameof(table));

        table = table.Trim();
        _tableName = table;
        return this;
    }

    public SuiteQLQueryBuilder Join(string table, string on)
    {
        if (string.IsNullOrEmpty(table)) throw new ArgumentException("Table name cannot be null or empty", nameof(table));
        if (string.IsNullOrEmpty(on)) throw new ArgumentException("Join condition cannot be null or empty", nameof(on));

        table = table.Trim();
        _joins.Add($"JOIN {table} ON {on}");
        return this;
    }

    public SuiteQLQueryBuilder LeftJoin(string table, string on)
    {
        if (string.IsNullOrEmpty(table)) throw new ArgumentException("Table name cannot be null or empty", nameof(table));
        if (string.IsNullOrEmpty(on)) throw new ArgumentException("Join condition cannot be null or empty", nameof(on));

        table = table.Trim();
        _joins.Add($"LEFT JOIN {table} ON {on}");
        return this;
    }

    public SuiteQLQueryBuilder InnerJoin(string table, string on)
    {
        if (string.IsNullOrEmpty(table)) throw new ArgumentException("Table name cannot be null or empty", nameof(table));
        if (string.IsNullOrEmpty(on)) throw new ArgumentException("Join condition cannot be null or empty", nameof(on));

        table = table.Trim();
        _joins.Add($"INNER JOIN {table} ON {on}");
        return this;
    }

    public SuiteQLQueryBuilder WithPropertyMap(string prop, string map)
    {
        if (string.IsNullOrEmpty(prop)) throw new ArgumentException("Property name cannot be null or empty", nameof(prop));
        if (string.IsNullOrEmpty(map)) throw new ArgumentException("Mapping name cannot be null or empty", nameof(map));

        PropertyMap[prop] = map;
        return this;
    }

    public SuiteQLQueryBuilder WithPropertyMap(params (string prop, string map)[] propMap)
    {
        foreach (var (prop, map) in propMap)
        {
            WithPropertyMap(prop, map);
        }  
        return this;
    }

    public record Literal(string Value);
}