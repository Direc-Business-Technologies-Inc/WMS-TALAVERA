using Ardalis.GuardClauses;

namespace Domain.Entities.Administration.User.Role;

public class RolePermissionDEM
{
    public int Id { get; private set; }
    public Guid ModulePermission { get; private set; }
    public Guid RoleId { get; private set; }
    RolePermissionDEM() { }
    public RolePermissionDEM(Guid modulePermission, Guid roleId)
    {
        ModulePermission = Guard.Against.Null(modulePermission, nameof(modulePermission), "Module Permission cannot be null or empty");
        RoleId = Guard.Against.Null(roleId, nameof(roleId), "Role ID cannot be null or empty");
    }
    public static RolePermissionDEM Create(Guid modulePermission, Guid roleId)
    {
        return new(modulePermission, roleId);
    }
}
