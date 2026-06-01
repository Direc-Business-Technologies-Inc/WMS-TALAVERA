using Application.DataTransferObjects.Commons;

namespace Application.DataTransferObjects.Administration.Role;

public class RoleDTO : AuditableDTO
{
    public bool Active { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
}
