using Shared.Entities;

namespace Web.BlazorServer.Helpers;

public class FilterValueTypeHelper
{
    public static FilterValueTypeEnum GetFilterValueTypeFromPropertyType(Type propertyType)
    {
        if (propertyType == typeof(string))
        {
            return FilterValueTypeEnum.String;
        }
        else if (propertyType == typeof(int) || propertyType == typeof(long) || propertyType == typeof(float) || propertyType == typeof(double) || propertyType == typeof(decimal) ||
                 propertyType == typeof(int?) || propertyType == typeof(long?) || propertyType == typeof(float?) || propertyType == typeof(double?) || propertyType == typeof(decimal?))
        {
            return FilterValueTypeEnum.Number;
        }
        else if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
        {
            return FilterValueTypeEnum.DateTime;
        }
        else
        {
            throw new NotSupportedException($"Property type '{propertyType.Name}' is not supported for filtering.");
        }
    }
}
