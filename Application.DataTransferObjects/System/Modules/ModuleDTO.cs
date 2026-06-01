using Application.DataTransferObjects.System.Security;
using Domain.ValueObjects.Others;

namespace Application.DataTransferObjects.System.Modules;

public class ModuleDTO
{
    public string Name { get; set; }
    public bool Active { get; set; }
    public string Code { get; set; }
    public bool Root { get; set; }
    public bool Transactional { get; set; }
    public Guid? NavRouteId { get; set; }
    public List<PermissionDTO> ModulePermissions { get; set; } = [];
}
