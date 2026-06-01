namespace Web.BlazorServer.ViewModels.Commons;

public abstract class EntityVM
{
    public Guid Id { get; set; } = Guid.NewGuid();
}
