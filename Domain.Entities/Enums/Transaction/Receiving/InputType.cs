using System.ComponentModel;

namespace Domain.Entities.Enums.Transaction.Receiving;

public enum InputType
{
    [Description("Manual")]
    Manual,

    [Description("Scanned")]
    Scanned
}
