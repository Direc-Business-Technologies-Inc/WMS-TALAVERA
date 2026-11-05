using Application.DataTransferObjects.Others.NS;
using Mapster;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger? _logger;

    public SuiteQLQueryBuilderFactoryService(ILogger<SuiteQLQueryBuilderFactoryService>? logger = null)
    {
        _logger = logger;
    }

    public SuiteQLQueryBuilder Create()
    {
        return new SuiteQLQueryBuilder(_logger);
    }
}

public class SuiteQLQueryBuilder
{
    private readonly ILogger? logger;
    const string DATETIME_FORMAT_STRING = "yyyy-MM-ddTHH:mm:ss";
    const string NETSUITE_DATETIME_FORMAT_STRING = "YYYY-MM-DD\"T\"HH24:MI:SS";
    /// <summary>NetSuite SuiteQL REST ceiling for the limit query parameter (valid range 1..1000).</summary>
    const int NETSUITE_MAX_LIMIT = 1000;
    public int? Take { get; set; }
    public int? Skip { get; set; }
    public List<AppFilterDescriptor> Filters { get; set; } = [];
    public List<AppSortDescriptor> Sorts { get; set; } = [];
    public List<(string col, string? alias)> SelectColumns { get; set; } = [];
    public Dictionary<string, string> PropertyMap { get; set; } = new();
    private string? _groupBy;
    private string? _tableName;
    private bool _isDistinct;
    private List<string> _joins = [];
    private HashSet<string> _uniqueColumns = new();

    public SuiteQLQueryBuilder() { }
    public SuiteQLQueryBuilder(ILogger? logger) { this.logger = logger; }
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

    /// <summary>
    /// Applies grid filters, sorts, and paging. <paramref name="mapFields"/>
    /// maps DTO/VM property names sent by the grid to SuiteQL column
    /// expressions — required for columns exposed through SELECT aliases
    /// (e.g. BUILTIN.DF(...)), because NetSuite does not resolve SELECT
    /// aliases in WHERE/ORDER BY and rejects them with 400 INVALID_PARAMETER
    /// "Unknown identifier". Unmapped properties are passed through verbatim,
    /// matching the previous behavior.
    /// </summary>
    public SuiteQLQueryBuilder WithDatagridIntent(DataGridIntent intent, Dictionary<string, string>? mapFields = null)
    {
        if (mapFields is not null)
        {
            foreach (var (key, value) in mapFields)
            {
                PropertyMap[key] = value;
            }
        }
        WithDataGridFilters(intent);
        WithDataGridSorts(intent);
        Take = intent.Take;
        Skip = intent.Skip;
        return this;
    }
    public SuiteQLQueryBuilder WithDataGridFilters(DataGridIntent intent)
    {
        foreach (var filter in intent.Filters)
        {
            MarkGridSourced(filter);
            Filters.Add(filter);
        }
        return this;
    }

    public SuiteQLQueryBuilder WithDataGridSorts(DataGridIntent intent)
    {
        foreach (var sort in intent.Sorts)
        {
            _gridSourcedDescriptors.Add(sort);
            Sorts.Add(sort);
        }
        return this;
    }

    /// <summary>
    /// Descriptors that arrived through a <see cref="DataGridIntent"/> (grid
    /// user actions or replayed persisted grid settings). Their property names
    /// are DTO/VM-facing, not SQL, so unresolved names are dropped at build
    /// time instead of emitted as raw identifiers NetSuite rejects. Hand-written
    /// filters added via WithFilters keep the legacy passthrough behavior.
    /// </summary>
    private readonly HashSet<object> _gridSourcedDescriptors = new();

    private void MarkGridSourced(AppFilterDescriptor filter)
    {
        _gridSourcedDescriptors.Add(filter);
        foreach (var child in filter.Filters)
        {
            MarkGridSourced(child);
        }
    }

    /// <summary>
    /// Resolves a sort/filter property name to a SuiteQL identifier.
    /// Order: explicit PropertyMap entry → SELECT alias (exact, then
    /// case-insensitive) → qualified column (contains '.') → raw passthrough
    /// for hand-written descriptors. Returns null only for grid-sourced
    /// descriptors that resolve to nothing — those are dropped rather than
    /// emitted, because NetSuite cannot resolve an unknown bare identifier.
    /// </summary>
    private string? ResolveGridProperty(string property, bool isGridSourced)
    {
        if (PropertyMap.TryGetValue(property, out var mapped)) return mapped;

        // Hand-written filters may target bare columns that are not SELECT
        // aliases (e.g. equality checks on non-selected columns); preserve them.
        if (property.Contains('.')) return property;

        var aliasMatch =
            SelectColumns.FirstOrDefault(c =>
                c.alias is not null &&
                string.Equals(c.alias, property, StringComparison.Ordinal));
        if (aliasMatch.alias is null)
        {
            aliasMatch = SelectColumns.FirstOrDefault(c =>
                c.alias is not null &&
                string.Equals(c.alias, property, StringComparison.OrdinalIgnoreCase));
        }
        if (aliasMatch.alias is not null) return aliasMatch.col;

        if (!isGridSourced) return property;
        return null;
    }

    private string BuildFilters()
    {
        if (Filters.Count == 0) return string.Empty;

        try
        {
            var parsed = Filters
                .Select(f => _parseFilter(f))
                .Where(f => f is not null)
                .ToList();
            if (parsed.Count == 0) return string.Empty;
            return " WHERE " + string.Join(" AND ", parsed);
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
            var parsed = Sorts
                .Select(s => _parseSort(s))
                .Where(s => s is not null)
                .ToList();
            if (parsed.Count == 0) return string.Empty;
            return " ORDER BY " + string.Join(", ", parsed);
        }
        catch (Exception ex)
        {
            throw new Exception($"Caught {ex.GetType().Name} while applying datagrid sorts: {ex.Message}", ex);
        }
    }

    private string BuildSelect()
    {
        var selectKeyword = _isDistinct ? "SELECT DISTINCT " : "SELECT ";

        if (SelectColumns.Count == 0) return "SELECT * ";
        return selectKeyword + string.Join(", ", SelectColumns.Select(c => c.alias != null ? $"{c.col} AS {c.alias}" : c.col));
    }

    private string BuildFrom()
    {
        if (string.IsNullOrEmpty(_tableName)) throw new InvalidOperationException("Table must be set");
        return $" FROM {_tableName} {string.Join(" ", _joins)}";
    }

    /// <summary>
    /// Builds the SuiteQL query, normalizing <see cref="Take"/> to the range NetSuite
    /// accepts for the <c>limit</c> query parameter (1..1000). NetSuite rejects anything
    /// outside that range with a 400 INVALID_PARAMETER, and callers reach this builder
    /// from grids (user-configurable page size), dropdowns, and hand-written intents —
    /// so the guarantee is enforced here, at the single chokepoint every builder-made
    /// query passes through, rather than trusted at each call site.
    /// <list type="bullet">
    /// <item>Below 1 (including the -1 "unlimited" convention and the reachable
    /// <c>Take = 0</c> from DataGridIntentAdapter) becomes <c>null</c>, which omits the
    /// query parameter entirely and lets NetSuite use its default page.</item>
    /// <item>Above 1000 is capped at 1000.</item>
    /// </list>
    /// Clamping happens before <c>SuiteQLQueryExtensions.ExecuteWithPaging</c> runs its
    /// modulo math, so non-null limits still get their offsets realigned by the existing
    /// stitching path (NetSuite requires offset to be divisible by limit).
    /// </summary>
    public SuiteQLQuery Build()
    {
        int? limit = Take;
        if (limit < 1) limit = null;
        else if (limit > NETSUITE_MAX_LIMIT) limit = NETSUITE_MAX_LIMIT;

        return new()
        {
            Query = BuildSelect() + BuildFrom() + BuildFilters() + BuildSorts() + _groupBy,
            Limit = limit,
            Offset = Skip
        };
    }

    private string? _parseSort(AppSortDescriptor sort, Dictionary<string, string>? propertyMap = null)
    {
        propertyMap ??= PropertyMap;

        var sortDir = sort.Direction switch
        {
            SortDirectionEnum.Descending => "DESC",
            SortDirectionEnum.Ascending => "ASC",
            _ => throw new ArgumentException("Invalid sort direction")
        };
        var property = ResolveGridProperty(sort.Property, _gridSourcedDescriptors.Contains(sort));
        if (property is null)
        {
            logger?.LogWarning(
                "Dropped grid sort on unmapped property '{Property}': it is neither in the property map, a SELECT alias, nor a qualified column, and would be rejected by NetSuite with 400 INVALID_PARAMETER.",
                sort.Property);
            return null;
        }
        return $"{property} {sortDir}";
    }

    private string? _parseFilter(AppFilterDescriptor filter, Dictionary<string, string>? propertyMap = null)
    {
        propertyMap ??= PropertyMap;

        // A group is a container: it holds no column of its own, only children.
        // Resolve it before its Property. Otherwise a group with an empty or
        // absent Property — e.g. the ItemDataGrid search OR-group, which sets
        // only LogicalOperator and Filters — resolves to null, is dropped whole,
        // and its children (valid ItemNumber/Name/Description filters) are never
        // parsed. The grid silently unfilters and keeps showing the prior rows.
        if (filter.Filters.Count > 0) return _parseFilterGroup(filter, propertyMap);

        var prop = ResolveGridProperty(filter.Property, _gridSourcedDescriptors.Contains(filter));
        if (prop is null)
        {
            logger?.LogWarning(
                "Dropped grid filter on unmapped property '{Property}': it is neither in the property map, a SELECT alias, nor a qualified column, and would be rejected by NetSuite with 400 INVALID_PARAMETER.",
                filter.Property);
            return null;
        }

        if (filter.ComparisonOperator == null) throw new InvalidOperationException($"no comparison operator given");
        switch (filter.ComparisonOperator)
        {
            case ComparisonOperatorEnum.Equals:
                return _binary(prop, "=", filter.Value);
            case ComparisonOperatorEnum.NotEquals :
                return _binary(prop, "!=", filter.Value);
            case ComparisonOperatorEnum.GreaterThan  :
                return _binary(prop, ">", filter.Value);
            case ComparisonOperatorEnum.GreaterThanOrEqual  :
                return _binary(prop, ">=", filter.Value);
            case ComparisonOperatorEnum.LessThan :
                return _binary(prop, "<", filter.Value);
            case ComparisonOperatorEnum.LessThanOrEqual  :
                return _binary(prop, "<=", filter.Value);
            case ComparisonOperatorEnum.Contains:
                return _stringOp(prop, ComparisonOperatorEnum.Contains, filter.Value);
            case ComparisonOperatorEnum.StartsWith:
                return _stringOp(prop, ComparisonOperatorEnum.StartsWith, filter.Value);
            case ComparisonOperatorEnum.EndsWith:
                return _stringOp(prop, ComparisonOperatorEnum.EndsWith, filter.Value);
            case ComparisonOperatorEnum.In :
                return _listOp(prop, "IN", filter.Value);
            case ComparisonOperatorEnum.NotIn :
                return _listOp(prop, "NOT IN", filter.Value);
            case ComparisonOperatorEnum.IsNull :
                return $"{prop} IS NULL ";
            case ComparisonOperatorEnum.IsNotNull :
                return $"{prop} IS NOT NULL ";
            case ComparisonOperatorEnum.IsEmpty  :
            case ComparisonOperatorEnum.IsNotEmpty:
                break;
        }

        throw new NotImplementedException($"Error at {nameof(SuiteQLQueryBuilder)}: {filter.ComparisonOperator} is not implemented ");
    }

    private string _stringifyValue(object value)
    {
        if (value is string strVal) return $"'{Helpers.SuiteQLText.EscapeLiteral(strVal)}'";
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

    private string _listOp(string prop, string op, object? value)
    {
        if (value is null || value is not IEnumerable) throw new InvalidOperationException($"operation {op} requires an array type as its value");

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

    private string _binary(string prop, string op, object? value)
    {
        if (value is null) throw new InvalidOperationException($"no value given for {prop}");

        if (value is DateTime dtVal) return $"TO_DATE({prop}, '{NETSUITE_DATETIME_FORMAT_STRING}') {op} TO_DATE({_stringifyValue(dtVal)}, '{NETSUITE_DATETIME_FORMAT_STRING}')";
        return $"{prop} {op} {_stringifyValue(value)}";
    }

    private string _stringOp(string prop, ComparisonOperatorEnum op, object? value)
    {
        if (value is null) throw new InvalidOperationException($"no value given for {prop}");
        if (value is not string strVal) throw new InvalidOperationException($"'{value}' is not a string and does not support {op}");

        // Literal text rides inside a quoted pattern, so escape quotes first —
        // an unescaped apostrophe (grid filter values are user input) breaks the
        // statement and NetSuite rejects it with a 400 INVALID_PARAMETER.
        var pattern = Helpers.SuiteQLText.EscapeLiteral(strVal);

        return op switch
        {
            ComparisonOperatorEnum.Contains => $"LOWER({prop}) LIKE LOWER('%{pattern}%')",
            ComparisonOperatorEnum.EndsWith => $"LOWER({prop}) LIKE LOWER('%{pattern}')",
            ComparisonOperatorEnum.StartsWith => $"LOWER({prop}) LIKE LOWER('{pattern}%')",
            _ => throw new NotImplementedException($"string operation {op} is not implemented in this version of wms")
        };
    }

    private string? _parseFilterGroup(AppFilterDescriptor filter, Dictionary<string, string>? propertyMap = null)
    {
        if (filter.LogicalOperator is null) throw new InvalidOperationException("Filter group given but no logical operator");
        string op = filter.LogicalOperator switch
        {
            LogicalOperatorEnum.AND => "AND",
            LogicalOperatorEnum.OR => "OR",
            _ => " AND "
        };

        // Drop unresolved children before joining: a dropped child parses to
        // null, and string.Join would emit an empty segment, producing a
        // malformed predicate such as "( OR LOWER(x) LIKE ...)" that NetSuite
        // rejects. An all-dropped group collapses to null like any other filter.
        var parts = filter.Filters
            .Select(child => _parseFilter(child, propertyMap))
            .Where(part => part is not null)
            .ToList();

        return parts.Count == 0 ? null : "(" + string.Join($" {op} ", parts) + ")";
    }

    public SuiteQLQueryBuilder Distinct()
    {
        _isDistinct = true;
        return this;
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

    public SuiteQLQueryBuilder GroupBy(string GroupBy, Dictionary<string, string>? propertyMap = null)
    {
        propertyMap ??= PropertyMap;

        _groupBy = " GROUP BY " + (propertyMap.ContainsKey(GroupBy) ? propertyMap[GroupBy] : GroupBy);

        return this;
    }
}