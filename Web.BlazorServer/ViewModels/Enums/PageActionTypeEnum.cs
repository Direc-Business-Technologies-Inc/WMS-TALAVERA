using System.ComponentModel;

namespace Web.BlazorServer.ViewModels.Enums;

public enum PageActionTypeEnum
{
    [Description("CREATE")]
    Create,
    [Description("UPDATE")]
    Update,
    [Description("VIEW")]
    View
}
