using System.ComponentModel;

namespace Domain.Enums;

public enum AuditType
{
    [Description("Create")]
    Create,

    [Description("Update")]
    Update,

    [Description("Soft Delete")]
    SoftDelete
}
