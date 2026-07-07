using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Administration.Settings;

public class SettingsDTO
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
}
