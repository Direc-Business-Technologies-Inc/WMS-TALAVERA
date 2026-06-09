using System.ComponentModel;

namespace Mobile.MAUI.Enums;

public class CustomEnum
{
    public enum PPCRole
    {
        [Description("Picker")]
        Picker = 1,

        [Description("Packer")]
        Packer = 2,

        [Description("Checker")]
        Checker = 3
    }

    public enum ModuleNavigation
    {
        [Description("Receiving")]
        Receiving = 1,
        [Description("ItemFulfillment")]
        ItemFulfillment = 2,

    }
}
