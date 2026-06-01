using Web.BlazorServer.ViewModels.Commons;

namespace Web.BlazorServer.ViewModels.Administration.Role;

public class RoleVM : AuditableVM
{
    public bool Active { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public List<RolePermissionVM> Permissions { get; set; } = [];
}
