using Application.DataTransferObjects.Commons;

namespace Application.DataTransferObjects.System.Commons;

public class DocumentTypeDTO : EntityDTO
{
    public int Code { get; set; }
    public string Name { get; set; } = string.Empty;
}
