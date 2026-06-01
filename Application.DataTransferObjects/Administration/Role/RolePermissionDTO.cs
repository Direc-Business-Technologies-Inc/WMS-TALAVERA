using Application.DataTransferObjects.System.Modules;

namespace Application.DataTransferObjects.Administration.Role;

public class RolePermissionDTO
{
    public int Id { get; set; }
    public ModulePermissionDTO Permission { get; set; }
    public Guid RoleId { get; set; }
}
