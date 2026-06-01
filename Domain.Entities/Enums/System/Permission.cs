using System.ComponentModel;

namespace Domain.Enums.System;

public enum Permission
{
    [Description("View")]
    View = 0,
    [Description("Create")]
    Create = 1,
    [Description("Edit")] 
    Edit = 2,
    [Description("Delete")] 
    Delete = 3,
    [Description("Approve")] 
    Approve = 4,
    [Description("Generate")] 
    Generate = 5,
}
