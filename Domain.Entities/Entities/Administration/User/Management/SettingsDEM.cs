using Ardalis.GuardClauses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Entities.Administration.User.Management;

public class SettingsDEM
{
    public Types Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public enum Types
    {
        String,
        Integer,
        Decimal
    }

    private SettingsDEM()
    {

    }

    public static string CheckType( Types type, string value)
    {
        if (string.IsNullOrEmpty(value)) throw new ArgumentNullException("value");
            switch (type)
        {
            case Types.String:
                return value;
            case Types.Integer:
                if (int.TryParse(value, out int intval)) return value;
                throw new Exception($"{value} is an invalid value for type {type}");
            case Types.Decimal:
                if (decimal.TryParse(value, out decimal decval)) return value;
                throw new Exception($"{value} is an invalid value for type {type}");
            default:
                throw new Exception($"Type {type} is not recognized");
        }
    }

    public static SettingsDEM Create(string code, string title, string description, Types type, string value)
    {
        title = Guard.Against.NullOrEmpty(title, nameof(title), "Please provide a title for the setting");
        code = Guard.Against.NullOrEmpty(code, nameof(code), "Please provide a code for the setting");
        description = Guard.Against.NullOrEmpty(description, nameof(description), "Please provide a description for the setting");
        type = Guard.Against.EnumOutOfRange(type, nameof(type), "Invalid settings type");
        value = CheckType(type, value);

        return new SettingsDEM()
        {
            Title = title,
            Code = code,
            Description = description,
            Type = type,
            Value = value
        };
    }
}
