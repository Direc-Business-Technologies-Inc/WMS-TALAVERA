using Web.BlazorServer.ViewModels.System;

namespace Web.BlazorServer.ViewModels.Administration.User;

public class UserPermissionVM
{
    public int Id { get; set; }
    public ModulePermissionVM Permission { get; set; }
    public Guid UserId { get; set; }
}
