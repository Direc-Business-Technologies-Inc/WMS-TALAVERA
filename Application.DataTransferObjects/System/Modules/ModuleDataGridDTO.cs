using Domain.Enums.System;

namespace Application.DataTransferObjects.System.Modules;

public class ModuleDataGridDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public bool Active { get; set; }
    public string Code { get; set; }
    public bool Root { get; set; }
    public bool Transactional { get; set; }
    public List<ModulePermissionDTO> Permissions { get; set; } = [];
}
