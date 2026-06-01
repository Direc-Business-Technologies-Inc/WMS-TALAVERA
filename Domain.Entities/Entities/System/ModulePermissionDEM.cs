using Ardalis.GuardClauses;
using Domain.Commons;
using Domain.ValueObjects.Others;

namespace Domain.Entities.System;

public class ModulePermissionDEM : EntityDEM
{
    public Guid ModuleReference { get; private set; }
    public PermissionVO Permission { get; private set; }

    public ModulePermissionDEM() { }

    public ModulePermissionDEM(Guid moduleReference, PermissionVO permission)
    {
        ModuleReference = Guard.Against.NullOrEmpty(moduleReference, nameof(ModulePermissionDEM.ModuleReference), "Module Reference cannot be null or empty");
        Permission = Guard.Against.Null(permission, nameof(ModulePermissionDEM.Permission), "Permission cannot be null");
    }


    public static ModulePermissionDEM Create(Guid moduleReference, PermissionVO permission)
    {
        return new ModulePermissionDEM(moduleReference, permission);
    }

    public ModulePermissionDEM SetModuleReference(Guid reference)
    {
        ModuleReference = Guard.Against.NullOrEmpty(reference, nameof(ModulePermissionDEM.ModuleReference), "Module Reference cannot be null or empty");

        return this;
    }

    public ModulePermissionDEM SetPermission(PermissionVO permission)
    {
        Permission = Guard.Against.Null(permission, nameof(ModulePermissionDEM.Permission), "Permission cannot be null or empty");

        return this;
    }
}
