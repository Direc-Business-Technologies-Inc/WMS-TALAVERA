using Application.DataTransferObjects.Commons;
using Application.DataTransferObjects.System.Commons;

namespace Application.DataTransferObjects.System;

public class DocumentNumberDTO : EntityDTO
{
    public DocumentTypeDTO DocumentType { get; set; }
    public string Code { get; set; }
    public string Prefix { get; set; }
    public int CurrentNumber { get; set; }
    public int NextNumber { get; set; }
    public string CurrentDocNum { get; set; }
    public string NextDocNum { get; set; }
}
