using Ardalis.GuardClauses;

namespace Domain.Entities.Administration.User.Management;

public class UserPermissionDEM
{
    public int Id { get; private set; }
    public Guid ModulePermission { get; private set; }
    public Guid UserId { get; private set; }
    UserPermissionDEM() { }
    public UserPermissionDEM(Guid modulePermission, Guid userId)
    {
        ModulePermission = Guard.Against.Null(modulePermission, nameof(modulePermission), "Module Permission cannot be null or empty");
        UserId = Guard.Against.Null(userId, nameof(userId), "User ID cannot be null or empty");
    }
    public static UserPermissionDEM Create(Guid modulePermission, Guid userId)
    {
        return new(modulePermission, userId);
    }
}
