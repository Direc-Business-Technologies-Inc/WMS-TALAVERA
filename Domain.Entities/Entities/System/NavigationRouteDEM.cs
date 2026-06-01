using Ardalis.GuardClauses;
using Domain.Commons;

namespace Domain.Entities.System;

public class NavigationRouteDEM : EntityDEM
{
    public string Name { get; private set; }
    public int? Position { get; private set; }
    public string? Icon { get; private set; }
    public string? Uri { get; private set; }
    public Guid? ParentId { get; set; }
    public bool Protected { get; private set; }
    public bool Active { get; private set; }

    public NavigationRouteDEM()
    {
        
    }

    NavigationRouteDEM(
        string name,
        bool protectedRoute = false,
        int? position = null,
        string? icon = null,
        string? uri = null,
        Guid? parentId = null)
    {
        Name = Guard.Against.NullOrEmpty(name, "Name");
        Protected = protectedRoute;
        Position = position;
        Icon = icon;
        Uri = uri;
        ParentId = parentId;
        Active = true;
    }
    public static NavigationRouteDEM New(
        string name,
        bool protectedRoute = false,
        int? position = null,
        string? icon = null,
        string? uri = null,
        Guid? parentId = null) => new(
            name,
            protectedRoute,
            position,
            icon,
            uri,
            parentId);

    public NavigationRouteDEM Update(
        string name,
        bool active,
        bool protectedRoute = false,
        int? position = null,
        string? icon = null,
        string? uri = null,
        Guid? parentId = null)
    {
        Name = Guard.Against.NullOrEmpty(name, "Name");
        Protected = protectedRoute;
        Position = position;
        Icon = icon;
        Uri = uri;
        ParentId = parentId;
        Active = active;

        return this;
    }


    public NavigationRouteDEM Activate()
    {
        Active = true;
        return this; 
    }

    public NavigationRouteDEM Deactivate()
    {
        Active = false;
        return this;
    }
}
