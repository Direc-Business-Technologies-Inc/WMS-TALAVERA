using Web.BlazorServer.ViewModels.Commons;

namespace Web.BlazorServer.ViewModels.System;

public class NavigationRouteVM : EntityVM
{
    public string Name { get; set; }
    public bool Protected { get; set; }
    public int? Position { get; set; } = 0;
    public string? Icon { get; set; } = null;
    public string? Uri { get; set; } = null;
    public NavigationRouteVM? Parent { get; set; } = null;
    public bool Active { get; set; } = true;
    public List<NavigationRouteVM> Children { get; set; } = [];
}
