using Web.BlazorServer.ViewModels.Commons;

namespace Web.BlazorServer.ViewModels.System;

public class ModulePermissionVM : EntityVM
{
    public Guid ModuleId { get; set; }
    public string ModuleCode { get; set; }
    public string Permission { get; set; }
}
