namespace Web.BlazorServer.ViewModels.Others;

public class LocationVM
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public int BinsCount { get; set; }
    public bool HasBins => BinsCount > 0;
}
