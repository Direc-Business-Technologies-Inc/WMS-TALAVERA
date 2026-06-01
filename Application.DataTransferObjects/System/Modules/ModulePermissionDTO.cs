using Application.DataTransferObjects.Commons;

namespace Application.DataTransferObjects.System.Modules;

public class ModulePermissionDTO : EntityDTO
{
    public Guid ModuleId { get; set; }
    public string ModuleCode { get; set; }
    public string Permission {  get; set; }
}
