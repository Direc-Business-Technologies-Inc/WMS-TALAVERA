namespace Application.DataTransferObjects.Commons;

public abstract class EntityDTO
{
    public Guid Id { get; set; } = Guid.NewGuid();
}
