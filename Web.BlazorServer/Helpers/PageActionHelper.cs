using Flurl.Util;
using Shared.Kernel;
using Web.BlazorServer.ViewModels.Enums;

namespace Web.BlazorServer.Helpers;

public class PageActionHelper
{
    public static PageActionTypeEnum GetPageActionType(string path)
    {
        string lastSegment = path.Split('/').Last();
        string action = lastSegment.SplitOnFirstOccurence("?").First();
        return EnumHelper.ParseStringToEnum<PageActionTypeEnum>(action);
    }
}
