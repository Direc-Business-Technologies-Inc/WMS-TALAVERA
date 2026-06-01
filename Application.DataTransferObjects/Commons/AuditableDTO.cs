
using Domain.Providers;

namespace Application.DataTransferObjects.Commons;

public class AuditableDTO : EntityDTO
{
    public Guid CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTimeProvider.UtcNow;
    public Guid? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public bool Archived { get; set; }
}
