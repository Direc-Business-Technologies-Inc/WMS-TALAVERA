namespace Web.BlazorServer.ViewModels.Commons;

public abstract class AuditableVM : EntityVM
{
    public Guid CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public Guid? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public bool Archived { get; set; }
}
