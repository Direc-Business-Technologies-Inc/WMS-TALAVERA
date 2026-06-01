namespace Web.BlazorServer.ViewModels.System;

public class ModuleDataGridVM
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public bool Active { get; set; }
    public string Code { get; set; }
    public bool Root { get; set; }
    public bool Transactional { get; set; }
    public List<ModulePermissionVM> Permissions { get; set; } = [];
    public bool HasPermission(string permission) => Permissions.Any(x => x.Permission.Equals(permission, StringComparison.CurrentCultureIgnoreCase));

}
