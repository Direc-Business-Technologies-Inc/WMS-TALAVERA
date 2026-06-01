using Shared.Entities;
using System.ComponentModel;
using System.Reflection;

namespace Database.Libraries.Helpers;

/// <summary>
/// Helper class for working with enum filters in DataGrid
/// </summary>
public class LinqEnumHelper
{
    /// <summary>
    /// Creates a filter descriptor for an enum property
    /// </summary>
    /// <typeparam name="TEnum">The enum type</typeparam>
    /// <param name="propertyName">The name of the property to filter</param>
    /// <param name="enumValue">The enum value to filter by</param>
    /// <param name="comparisonOperator">The comparison operator (default: Equals)</param>
    /// <returns>An AppFilterDescriptor configured for enum filtering</returns>
    public static AppFilterDescriptor CreateEnumFilter<TEnum>(
        string propertyName,
        TEnum enumValue,
        ComparisonOperatorEnum comparisonOperator = ComparisonOperatorEnum.Equals)
        where TEnum : struct, Enum
    {
        return new AppFilterDescriptor
        {
            Property = propertyName,
            Value = enumValue,
            FilterValueType = FilterValueTypeEnum.Enum,
            ComparisonOperator = comparisonOperator
        };
    }

    /// <summary>
    /// Creates a filter descriptor for multiple enum values using In operator
    /// </summary>
    /// <typeparam name="TEnum">The enum type</typeparam>
    /// <param name="propertyName">The name of the property to filter</param>
    /// <param name="enumValues">The enum values to include</param>
    /// <returns>An AppFilterDescriptor configured for enum In filtering</returns>
    public static AppFilterDescriptor CreateEnumInFilter<TEnum>(
        string propertyName,
        params TEnum[] enumValues)
        where TEnum : struct, Enum
    {
        return new AppFilterDescriptor
        {
            Property = propertyName,
            Value = enumValues.Cast<object>().ToList(),
            FilterValueType = FilterValueTypeEnum.Enum,
            ComparisonOperator = ComparisonOperatorEnum.In
        };
    }

    /// <summary>
    /// Creates a filter descriptor for excluding enum values using NotIn operator
    /// </summary>
    /// <typeparam name="TEnum">The enum type</typeparam>
    /// <param name="propertyName">The name of the property to filter</param>
    /// <param name="enumValues">The enum values to exclude</param>
    /// <returns>An AppFilterDescriptor configured for enum NotIn filtering</returns>
    public static AppFilterDescriptor CreateEnumNotInFilter<TEnum>(
        string propertyName,
        params TEnum[] enumValues)
        where TEnum : struct, Enum
    {
        return new AppFilterDescriptor
        {
            Property = propertyName,
            Value = enumValues.Cast<object>().ToList(),
            FilterValueType = FilterValueTypeEnum.Enum,
            ComparisonOperator = ComparisonOperatorEnum.NotIn
        };
    }

    /// <summary>
    /// Gets all enum values for a given enum type
    /// </summary>
    /// <typeparam name="TEnum">The enum type</typeparam>
    /// <returns>Array of all enum values</returns>
    public static TEnum[] GetAllEnumValues<TEnum>() where TEnum : struct, Enum
    {
        return Enum.GetValues<TEnum>();
    }

    /// <summary>
    /// Gets the description attribute value for an enum value
    /// </summary>
    /// <param name="enumValue">The enum value</param>
    /// <returns>Description if available, otherwise enum name</returns>
    public static string GetEnumDescription(Enum enumValue)
    {
        var field = enumValue.GetType().GetField(enumValue.ToString());
        if (field == null) return enumValue.ToString();

        var attribute = field.GetCustomAttribute<DescriptionAttribute>();
        return attribute?.Description ?? enumValue.ToString();
    }

    /// <summary>
    /// Parses a string value to an enum, supporting both name and description
    /// </summary>
    /// <typeparam name="TEnum">The enum type</typeparam>
    /// <param name="value">String value to parse</param>
    /// <param name="ignoreCase">Whether to ignore case</param>
    /// <returns>Parsed enum value</returns>
    public static TEnum ParseEnum<TEnum>(string value, bool ignoreCase = true)
        where TEnum : struct, Enum
    {
        // Try direct parse first
        if (Enum.TryParse<TEnum>(value, ignoreCase, out var result))
            return result;

        // Try by description
        foreach (TEnum enumValue in Enum.GetValues<TEnum>())
        {
            var description = GetEnumDescription(enumValue);
            if (string.Equals(description, value,
                ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            {
                return enumValue;
            }
        }

        throw new ArgumentException($"Unable to parse '{value}' to {typeof(TEnum).Name}");
    }

    /// <summary>
    /// Creates a list of filter options for an enum suitable for UI dropdowns
    /// </summary>
    /// <typeparam name="TEnum">The enum type</typeparam>
    /// <returns>List of tuples with enum value, name, and description</returns>
    public static List<(TEnum Value, string Name, string Description)> GetEnumFilterOptions<TEnum>()
        where TEnum : struct, Enum
    {
        return Enum.GetValues<TEnum>()
            .Select(e => (
                Value: e,
                Name: e.ToString(),
                Description: GetEnumDescription(e)
            ))
            .ToList();
    }
}
