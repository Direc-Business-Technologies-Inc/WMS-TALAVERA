using System.ComponentModel;
using System.Reflection;

namespace Shared.Kernel;

public class EnumHelper
{
    /// <summary>
    /// Retrieves the Description attribute of an enum value.
    /// </summary>
    /// <param name="value">The enum value.</param>
    /// <returns>The description if available; otherwise, the enum value as a string.</returns>
    public static string GetEnumDescription(Enum value)
    {
        FieldInfo field = value.GetType().GetField(value.ToString());
        DescriptionAttribute attribute = field.GetCustomAttribute<DescriptionAttribute>();

        return attribute != null ? attribute.Description : value.ToString();
    }

    public static T ParseStringToEnum<T>(string value) where T : Enum
    {
        return (T)Enum.Parse(typeof(T), value, true);
    }
}
