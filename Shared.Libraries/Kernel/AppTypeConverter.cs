using System.Globalization;
using System.Reflection;

namespace Shared.Kernel;

public class AppTypeConverter
{
    public static dynamic Convert(string value, string targetType)
    {
        var typeConverters = new Dictionary<string, Func<string, object>>
    {
        { "string", val => val },
        { "int", val => int.Parse(val, CultureInfo.InvariantCulture) },
        { "decimal", val => decimal.Parse(val, CultureInfo.InvariantCulture) },
        { "double", val => double.Parse(val, CultureInfo.InvariantCulture) },
        { "datetime", val => DateTime.Parse(val, CultureInfo.InvariantCulture) },
        { "bool", val => bool.Parse(val) },
        { "guid", val => Guid.Parse(val) },
        { "string_list", val => val.Split(',').Select(x => x.Trim()).ToArray() }
    };

        if (typeConverters.TryGetValue(targetType.ToLower(), out var converter))
        {
            try
            {
                return converter(value);
            }
            catch (Exception)
            {
                throw;
            }
        }

        throw new NotSupportedException($"Conversion to {targetType} is not supported.");
    }

    public static string CSharpTypeToSqlType(string type)
    {
        switch (type.Trim().ToLower())
        {
            case "string":
                return "NVARCHAR(128)";
            case "bool":
            case "boolean":
                return "BIT";
            case "int":
                return "int";
            case "decimal":
                return "DECIMAL(18,5)";
            case "datetime":
                return "DATETIME";
            case "guid":
                return "UNIQUEIDENTIFIER";
            case "char":
                return "NCHAR(1)";
            default:
                return "NVARCHAR(128)";
        }
    }

    public static Type GetCSharpType(string typeName)
    {
        switch (typeName.Trim().ToLower())
        {
            case "string":
                return typeof(string);
            case "bool":
            case "boolean":
                return typeof(bool);
            case "int":
                return typeof(int);
            case "decimal":
                return typeof(decimal);
            case "datetime":
                return typeof(DateTime);
            case "guid":
                return typeof(Guid);
            case "char":
                return typeof(char);
            default:
                return typeof(string);
        }
    }

    public static List<Dictionary<string, object>> QueryToDictionary(IEnumerable<dynamic> data)
    {
        if (data is null)
            return new();

        var list = new List<Dictionary<string, object>>();

        foreach (var row in data)
        {
            var dict = new Dictionary<string, object>();

            foreach (var kvp in (IDictionary<string, object>)row)
            {
                dict[kvp.Key] = kvp.Value;
            }

            list.Add(dict);
        }

        return list;
    }

    public static Dictionary<string, object> ToDictionary(object obj)
    {
        var dict = new Dictionary<string, object>();

        if (obj == null) return dict;

        foreach (PropertyInfo property in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            dict[property.Name] = property.GetValue(obj)!;
        }

        return dict;
    }

    public static T FromDictionary<T>(T obj, Dictionary<string, object> dictionary, bool ignoreCase = false)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));

        Type type = obj.GetType();
        BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance;
        if (ignoreCase) bindingFlags |= BindingFlags.IgnoreCase;

        foreach (var kvp in dictionary)
        {
            try
            {
                PropertyInfo property = type.GetProperty(kvp.Key, bindingFlags);

                if (property != null && property.CanWrite)
                {
                    object convertedValue = ObjectToTyped(kvp.Value, property.PropertyType);
                    property.SetValue(obj, convertedValue);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting property '{kvp.Key}': {ex.Message}");
            }
        }

        return obj;
    }

    private static object ObjectToTyped(object value, Type targetType)
    {
        if (value == null)
        {
            if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
            {
                throw new InvalidOperationException($"Cannot assign null to non-nullable type {targetType.Name}");
            }

            return null;
        }

        Type nullableType = Nullable.GetUnderlyingType(targetType);
        if (nullableType != null)
        {
            targetType = nullableType;
        }

        if (targetType.IsAssignableFrom(value.GetType()))
        {
            return value;
        }

        if (targetType.IsEnum)
        {
            if (value is string stringValue)
            {
                return Enum.Parse(targetType, stringValue, true);
            }
            return Enum.ToObject(targetType, value);
        }

        if (targetType == typeof(Guid))
        {
            return Guid.Parse(value.ToString());
        }

        if (value is IConvertible)
        {
            return System.Convert.ChangeType(value, targetType);
        }

        return null;
    }

    public static string ToOVBLTypes(Type type)
    {
        if (type == null || type == typeof(string) || type.BaseType == typeof(Enum))
            return "STRING";
        if (type == typeof(int))
            return "INT";
        if (type == typeof(decimal))
            return "DECIMAL";
        if (type == typeof(double))
            return "DECIMAL";
        if (type == typeof(DateTime))
            return "DATETIME";
        if (type == typeof(bool))
            return "BOOL";
        if (type == typeof(Guid))
            return "GUID";
        throw new NotSupportedException($"Type {type.Name} is not supported.");
    }
}
