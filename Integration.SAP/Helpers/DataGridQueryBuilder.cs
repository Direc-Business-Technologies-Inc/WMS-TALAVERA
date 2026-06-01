using Shared.Entities;
using System.Text;

namespace Integration.Sap.Helpers;

public static class DataGridQueryBuilder
{
    public static string BuildQuery(string baseQuery, DataGridIntent intent)
    {
        var where = BuildWhereClause(intent.Filters);
        var orderBy = BuildSortClause(intent.Sorts);
        var paging = BuildPaging(intent.Skip, intent.Take);

        var sb = new StringBuilder();
        sb.AppendLine("WITH QueryResult AS (");
        sb.AppendLine(baseQuery);
        sb.AppendLine(")");
        sb.AppendLine("SELECT *");
        sb.AppendLine("FROM QueryResult");

        if (!string.IsNullOrWhiteSpace(where))
            sb.AppendLine("WHERE " + where);

        if (!string.IsNullOrWhiteSpace(orderBy))
            sb.AppendLine(orderBy);

        if (!string.IsNullOrWhiteSpace(paging))
            sb.AppendLine(paging);

        return sb.ToString();
    }

    // -----------------------------------------------------
    // WHERE CLAUSE
    // -----------------------------------------------------

    private static string BuildWhereClause(List<AppFilterDescriptor> filters)
    {
        if (filters == null || filters.Count == 0)
            return string.Empty;

        var parts = filters.Select(BuildFilter).Where(x => !string.IsNullOrWhiteSpace(x));
        return string.Join(" AND ", parts);
    }

    private static string BuildFilter(AppFilterDescriptor f)
    {
        if (f == null)
            return string.Empty;

        // Handle nested groups
        if (f.Filters != null && f.Filters.Count > 0)
        {
            var op = f.LogicalOperator == LogicalOperatorEnum.OR ? "OR" : "AND";

            var inner = f.Filters
                .Select(BuildFilter)
                .Where(x => !string.IsNullOrWhiteSpace(x));

            return "(" + string.Join($" {op} ", inner) + ")";
        }

        if (string.IsNullOrWhiteSpace(f.Property))
            return string.Empty;

        return BuildComparison(f);
    }

    private static string BuildComparison(AppFilterDescriptor f)
    {
        string column = f.Property;
        string sqlValue = FormatValue(f.Value, f.FilterValueType);

        return f.ComparisonOperator switch
        {
            ComparisonOperatorEnum.Equals => $"{column} = {sqlValue}",
            ComparisonOperatorEnum.NotEquals => $"{column} <> {sqlValue}",
            ComparisonOperatorEnum.GreaterThan => $"{column} > {sqlValue}",
            ComparisonOperatorEnum.GreaterThanOrEqual => $"{column} >= {sqlValue}",
            ComparisonOperatorEnum.LessThan => $"{column} < {sqlValue}",
            ComparisonOperatorEnum.LessThanOrEqual => $"{column} <= {sqlValue}",

            ComparisonOperatorEnum.Contains => $"{column} LIKE '%' + {sqlValue} + '%'",
            ComparisonOperatorEnum.StartsWith => $"{column} LIKE {sqlValue} + '%'",
            ComparisonOperatorEnum.EndsWith => $"{column} LIKE '%' + {sqlValue}",

            ComparisonOperatorEnum.IsEmpty => $"{column} = ''",
            ComparisonOperatorEnum.IsNotEmpty => $"{column} <> ''",

            ComparisonOperatorEnum.IsNull => $"{column} IS NULL",
            ComparisonOperatorEnum.IsNotNull => $"{column} IS NOT NULL",

            ComparisonOperatorEnum.In => $"{column} IN ({FormatListValue(f.Value)})",
            ComparisonOperatorEnum.NotIn => $"{column} NOT IN ({FormatListValue(f.Value)})",

            _ => ""
        };
    }

    private static string FormatValue(object? value, FilterValueTypeEnum type)
    {
        if (value == null)
            return "NULL";

        return type switch
        {
            FilterValueTypeEnum.String => $"'{value.ToString().Replace("'", "''")}'",
            FilterValueTypeEnum.Number => $"{value}",
            FilterValueTypeEnum.DateTime => $"'{((DateTime)value):yyyy-MM-dd HH:mm:ss}'",
            _ => $"'{value}'"
        };
    }

    private static string FormatListValue(object? value)
    {
        if (value is IEnumerable<object> list)
        {
            return string.Join(", ", list.Select(v => $"'{v}'"));
        }

        return $"'{value}'";
    }

    // -----------------------------------------------------
    // ORDER BY
    // -----------------------------------------------------

    private static string BuildSortClause(List<AppSortDescriptor> sorts)
    {
        if (sorts == null || sorts.Count == 0)
            return string.Empty;

        var items = sorts.Select(s =>
            $"{s.Property} {(s.Direction == SortDirectionEnum.Ascending ? "ASC" : "DESC")}"
        );

        return "ORDER BY " + string.Join(", ", items);
    }

    // -----------------------------------------------------
    // PAGING
    // -----------------------------------------------------

    private static string BuildPaging(int skip, int take)
    {
        if (skip < 0) skip = 0;
        if (take <= 0) return "";

        return $"OFFSET {skip} ROWS FETCH NEXT {take} ROWS ONLY";
    }


    public static string BuildCountQuery(
        string baseQuery,
        List<AppFilterDescriptor> filters,
        Dictionary<string, string> columnMap)
    {
        var filterClause = BuildCountQueryWhereClause(filters, columnMap);

        var sb = new StringBuilder();
        sb.AppendLine("SELECT COUNT(1) AS Count");
        sb.AppendLine("FROM (");
        sb.AppendLine(baseQuery.Trim());

        if (!string.IsNullOrWhiteSpace(filterClause))
        {
            bool hasWhere = baseQuery.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase) >= 0;
            sb.AppendLine(hasWhere ? " AND " + filterClause : " WHERE " + filterClause);
        }

        sb.AppendLine(") AS QueryResult"); // alias required for subquery
        return sb.ToString();
    }

    // -------------------- FILTER BUILDER --------------------

    private static string BuildCountQueryWhereClause(List<AppFilterDescriptor> filters, Dictionary<string, string> columnMap)
    {
        if (filters == null || filters.Count == 0)
            return string.Empty;

        var parts = filters.Select(f => BuildCountQueryFilter(f, columnMap))
                           .Where(x => !string.IsNullOrWhiteSpace(x));

        return string.Join(" AND ", parts);
    }

    private static string BuildCountQueryFilter(AppFilterDescriptor f, Dictionary<string, string> columnMap)
    {
        if (f == null) return string.Empty;

        // Nested group
        if (f.Filters != null && f.Filters.Count > 0)
        {
            var op = f.LogicalOperator == LogicalOperatorEnum.OR ? "OR" : "AND";
            var inner = f.Filters.Select(x => BuildCountQueryFilter(x, columnMap))
                                 .Where(x => !string.IsNullOrWhiteSpace(x));
            return "(" + string.Join($" {op} ", inner) + ")";
        }

        if (string.IsNullOrWhiteSpace(f.Property))
            return string.Empty;

        // Map property to actual table column
        string column = columnMap.ContainsKey(f.Property) ? columnMap[f.Property] : f.Property;
        return BuildComparison(f, column);
    }

    private static string BuildComparison(AppFilterDescriptor f, string column)
    {
        string sqlValue = FormatCountQueryValue(f.Value, f.FilterValueType);

        return f.ComparisonOperator switch
        {
            ComparisonOperatorEnum.Equals => $"{column} = {sqlValue}",
            ComparisonOperatorEnum.NotEquals => $"{column} <> {sqlValue}",
            ComparisonOperatorEnum.GreaterThan => $"{column} > {sqlValue}",
            ComparisonOperatorEnum.GreaterThanOrEqual => $"{column} >= {sqlValue}",
            ComparisonOperatorEnum.LessThan => $"{column} < {sqlValue}",
            ComparisonOperatorEnum.LessThanOrEqual => $"{column} <= {sqlValue}",

            ComparisonOperatorEnum.Contains => $"{column} LIKE '%' + {sqlValue} + '%'",
            ComparisonOperatorEnum.StartsWith => $"{column} LIKE {sqlValue} + '%'",
            ComparisonOperatorEnum.EndsWith => $"{column} LIKE '%' + {sqlValue}",

            ComparisonOperatorEnum.IsNull => $"{column} IS NULL",
            ComparisonOperatorEnum.IsNotNull => $"{column} IS NOT NULL",

            ComparisonOperatorEnum.IsEmpty => $"{column} = ''",
            ComparisonOperatorEnum.IsNotEmpty => $"{column} <> ''",

            ComparisonOperatorEnum.In => $"{column} IN ({FormatListValue(f.Value)})",
            ComparisonOperatorEnum.NotIn => $"{column} NOT IN ({FormatListValue(f.Value)})",

            _ => ""
        };
    }

    private static string FormatCountQueryValue(object? value, FilterValueTypeEnum type)
    {
        if (value == null) return "NULL";

        return type switch
        {
            FilterValueTypeEnum.String => $"'{value.ToString().Replace("'", "''")}'",
            FilterValueTypeEnum.Number => $"{value}",
            FilterValueTypeEnum.DateTime => $"'{((DateTime)value):yyyy-MM-dd HH:mm:ss}'",
            _ => $"'{value}'"
        };
    }

    private static string FormatCountQueryListValue(object? value)
    {
        if (value is IEnumerable<object> list)
            return string.Join(", ", list.Select(v => $"'{v}'"));

        return $"'{value}'";
    }


}
