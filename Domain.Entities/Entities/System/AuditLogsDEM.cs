using Ardalis.GuardClauses;
using Domain.Commons;

namespace Domain.Entities.System;

public class AuditLogsDEM : EntityDEM
{
    public string Module { get; private set; }
    public string Action { get; private set; }
    public DateTime Created { get; private set; }
    public Guid SetBy { get; private set; }

    AuditLogsDEM() { }

    AuditLogsDEM(
        string module,
        string action,
        DateTime created,
        Guid setBy)
    {
        Module = module;
        Action = action;
        Created = created;
        SetBy = setBy;
    }

    public static AuditLogsDEM Create(string module, string action, DateTime created, Guid setBy)
    {
        AuditLogsDEM dem = new(
            Guard.Against.NullOrEmpty(module, nameof(module), "Module cannot be empty or null"),
            Guard.Against.NullOrEmpty(action, nameof(action), "Action cannot be empty or null"),
            Guard.Against.Null(created, nameof(created), "Created cannot be empty or null"),
            Guard.Against.NullOrEmpty(setBy, nameof(setBy), "Set by cannot be empty or null"));
        return dem;
    }
}
