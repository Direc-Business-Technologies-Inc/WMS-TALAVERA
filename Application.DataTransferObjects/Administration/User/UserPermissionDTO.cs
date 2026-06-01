using Application.DataTransferObjects.System.Modules;

namespace Application.DataTransferObjects.Administration.User;

public class UserPermissionDTO
{
    public int Id { get; set; }
    public ModulePermissionDTO Permission { get; set; }
    public Guid UserId { get; set; }
}