using Application.DataTransferObjects.Commons;

namespace Application.DataTransferObjects.System.Commons;

public class NavigationRouteDTO : EntityDTO
{
    public string Name { get; set; }
    public bool Protected { get; set; }
    public int? Position { get; set; } = 0;
    public string? Icon { get; set; } = null;
    public string? Uri { get; set; } = null;
    public Guid? ParentId { get; set; } = null;
    public bool Active { get; set; } = true;
}
