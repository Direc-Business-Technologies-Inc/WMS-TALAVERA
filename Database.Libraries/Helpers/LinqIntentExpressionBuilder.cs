using Shared.Entities;
using System.Linq.Expressions;

namespace Database.Libraries.Helpers;

public static class LinqIntentExpressionBuilder
{
    public static Expression<Func<TEntity, bool>> BuildPredicate<TEntity>(
       List<AppFilterDescriptor> filters)
    {
        if (filters == null || filters.Count == 0)
            return x => true; // no filters = allow all

        var parameter = Expression.Parameter(typeof(TEntity), "x");

        Expression? finalExpr = null;

        foreach (var filter in filters)
        {
            var expr = BuildExpression<TEntity>(filter, parameter);
            finalExpr = finalExpr == null ? expr : Expression.AndAlso(finalExpr, expr);
        }

        return Expression.Lambda<Func<TEntity, bool>>(finalExpr!, parameter);
    }

    private static Expression BuildExpression<TEntity>(AppFilterDescriptor filter, ParameterExpression parameter)
    {
        // Nested logical groups (filters inside filters)
        if (filter.Filters?.Count > 0)
            return BuildGroupExpression<TEntity>(filter, parameter);

        // Simple filter
        var property = Expression.Property(parameter, filter.Property);

        // Automatically detect if property is an enum type
        var propertyType = Nullable.GetUnderlyingType(property.Type) ?? property.Type;
        var isEnum = propertyType.IsEnum;

        // For enum properties, only certain operators are valid
        if (isEnum)
        {
            return filter.ComparisonOperator switch
            {
                ComparisonOperatorEnum.Equals => BuildEnumEquals(property, filter.Value),
                ComparisonOperatorEnum.NotEquals => BuildEnumNotEquals(property, filter.Value),
                ComparisonOperatorEnum.In => BuildListContains(property, filter.Value),
                ComparisonOperatorEnum.NotIn => Expression.Not(BuildListContains(property, filter.Value)),
                _ => throw new NotSupportedException($"Operator {filter.ComparisonOperator} is not supported for enum properties. Use Equals, NotEquals, In, or NotIn.")
            };
        }

        // Non-enum properties (original logic)
        var constant = Expression.Constant(ConvertValue(property.Type, filter.Value), property.Type);

        return filter.ComparisonOperator switch
        {
            ComparisonOperatorEnum.Equals => Expression.Equal(property, constant),
            ComparisonOperatorEnum.NotEquals => Expression.NotEqual(property, constant),
            ComparisonOperatorEnum.GreaterThan => Expression.GreaterThan(property, constant),
            ComparisonOperatorEnum.GreaterThanOrEqual => Expression.GreaterThanOrEqual(property, constant),
            ComparisonOperatorEnum.LessThan => Expression.LessThan(property, constant),
            ComparisonOperatorEnum.LessThanOrEqual => Expression.LessThanOrEqual(property, constant),
            ComparisonOperatorEnum.Contains => BuildStringMethod(property, "Contains", constant),
            ComparisonOperatorEnum.StartsWith => BuildStringMethod(property, "StartsWith", constant),
            ComparisonOperatorEnum.EndsWith => BuildStringMethod(property, "EndsWith", constant),
            ComparisonOperatorEnum.IsEmpty => Expression.Equal(property, Expression.Constant("")),
            ComparisonOperatorEnum.IsNotEmpty => Expression.NotEqual(property, Expression.Constant("")),
            ComparisonOperatorEnum.In => BuildListContains(property, filter.Value),
            ComparisonOperatorEnum.NotIn => Expression.Not(BuildListContains(property, filter.Value)),
            _ => throw new NotSupportedException($"Unsupported comparison: {filter.ComparisonOperator}")
        };
    }

    private static Expression BuildGroupExpression<TEntity>(AppFilterDescriptor group, ParameterExpression parameter)
    {
        Expression? result = null;

        foreach (var f in group.Filters)
        {
            var expr = BuildExpression<TEntity>(f, parameter);

            result = result == null
                ? expr
                : group.LogicalOperator == LogicalOperatorEnum.OR
                    ? Expression.OrElse(result, expr)
                    : Expression.AndAlso(result, expr);
        }

        return result!;
    }

    private static Expression BuildEnumEquals(MemberExpression property, object? value)
    {
        var propertyType = Nullable.GetUnderlyingType(property.Type) ?? property.Type;
        var convertedValue = ConvertToEnum(propertyType, value);
        var constant = Expression.Constant(convertedValue, property.Type);
        return Expression.Equal(property, constant);
    }

    private static Expression BuildEnumNotEquals(MemberExpression property, object? value)
    {
        var propertyType = Nullable.GetUnderlyingType(property.Type) ?? property.Type;
        var convertedValue = ConvertToEnum(propertyType, value);
        var constant = Expression.Constant(convertedValue, property.Type);
        return Expression.NotEqual(property, constant);
    }

    private static object? ConvertToEnum(Type enumType, object? value)
    {
        if (value == null) return null;

        // Handle string-based enum values
        if (value is string stringValue)
            return Enum.Parse(enumType, stringValue, ignoreCase: true);

        // Handle numeric values (int, long, etc.)
        if (value.GetType().IsPrimitive || value is decimal)
            return Enum.ToObject(enumType, value);

        // Already the correct enum type
        if (value.GetType() == enumType)
            return value;

        // Try to convert
        return Enum.ToObject(enumType, Convert.ChangeType(value, Enum.GetUnderlyingType(enumType)));
    }

    private static object? ConvertValue(Type targetType, object? value)
    {
        if (value == null) return null;

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlyingType == typeof(string)) return value.ToString();

        if (underlyingType.IsEnum)
        {
            // Handle both string and numeric enum values
            if (value is string stringValue)
                return Enum.Parse(underlyingType, stringValue, ignoreCase: true);

            // Handle numeric values (int, long, etc.)
            return Enum.ToObject(underlyingType, value);
        }

        return Convert.ChangeType(value, underlyingType);
    }

    private static Expression BuildStringMethod(MemberExpression property, string methodName, ConstantExpression constant)
    {
        return Expression.Call(property, typeof(string).GetMethod(methodName, new[] { typeof(string) })!, constant);
    }

    private static Expression BuildListContains(MemberExpression property, object? value)
    {
        var propertyType = Nullable.GetUnderlyingType(property.Type) ?? property.Type;

        // Convert the list values to the correct type
        List<object> valuesList = [];


        if (propertyType.IsEnum)
        {
            valuesList = [.. ((System.Collections.IEnumerable)value!).Cast<object>()];

            // Create a typed list for enums
            var enumListType = typeof(List<>).MakeGenericType(propertyType);
            var typedList = Activator.CreateInstance(enumListType);
            var addMethod = enumListType.GetMethod("Add")!;

            foreach (var item in valuesList)
            {
                var convertedValue = item is string str
                    ? Enum.Parse(propertyType, str, ignoreCase: true)
                    : Enum.ToObject(propertyType, item);
                addMethod.Invoke(typedList, new[] { convertedValue });
            }

            var constant = Expression.Constant(typedList);
            var containsMethod = enumListType.GetMethod("Contains", new[] { propertyType })!;

            // Handle nullable enums
            var propertyExpression = property.Type != propertyType
                ? Expression.Property(property, "Value")
                : (Expression)property;

            return Expression.Call(constant, containsMethod, propertyExpression);
        }
        else
        {
            valuesList = [.. ((IEnumerable<object>)value!)];

            // Original implementation for non-enum types
            var constant = Expression.Constant(valuesList);
            var containsMethod = typeof(List<object>).GetMethod("Contains")!;
            return Expression.Call(constant, containsMethod, Expression.Convert(property, typeof(object)));
        }
    }
}