using System.ComponentModel;

namespace Shared.Entities;

public class AppFilterDescriptor
{
    public LogicalOperatorEnum? LogicalOperator { get; set; } = null;
    public string Property { get; set; } = string.Empty;
    public object? Value { get; set; }
    public FilterValueTypeEnum FilterValueType { get; set; }
    public ComparisonOperatorEnum? ComparisonOperator { get; set; } = null;
    public List<AppFilterDescriptor> Filters { get; set; } = [];
}

public enum LogicalOperatorEnum
{
    AND = 0,
    OR = 1
}

public enum FilterValueTypeEnum
{
    String,
    Number,
    DateTime,
    Boolean,
    Enum
}

public enum ComparisonOperatorEnum
{
    Equals = 0,
    NotEquals = 1,
    GreaterThan = 2,
    GreaterThanOrEqual = 3,
    LessThan = 4,
    LessThanOrEqual = 5,
    Contains = 6,
    StartsWith = 7,
    EndsWith = 8,
    IsEmpty = 9,
    IsNotEmpty = 10,
    In = 11,
    NotIn = 12,
    IsNull = 13,
    IsNotNull = 14,
}

public class AppSortDescriptor
{
    public string Property { get; set; } = string.Empty;
    public SortDirectionEnum Direction { get; set; } = SortDirectionEnum.Ascending;

}

public enum SortDirectionEnum
{
    [Description("ASC")]
    Ascending,
    [Description("DESC")]
    Descending
}
