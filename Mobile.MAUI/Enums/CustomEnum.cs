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
        [Description("Packing")]
        Packing = 2,
        [Description("TripTicket")]
        TripTicket = 3,
        [Description("InventoryCounting")]
        InventoryCounting = 4,
        [Description("InventoryWorksheet")]
        InventoryWorksheet = 5,
    }

    public enum ToggleState
    {
        Base = 0,
        Good = 1,
        Bad = 2
    }
}
