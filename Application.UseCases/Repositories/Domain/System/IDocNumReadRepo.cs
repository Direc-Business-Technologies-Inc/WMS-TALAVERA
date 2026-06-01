using Application.DataTransferObjects.System;
using Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Application.UseCases.Repositories.Domain.System;

public interface IDocNumReadRepo
{
    Task<DocumentNumberDTO> GetDocumentNumberWithLockingAsync(Guid documentTypeId);
    Task<DocumentNumberDTO> GetDocumentNumberWithLockingAsync(Guid documentTypeId, DbContext context);
    Task<DocumentNumberDEM> GetDocumentNumberEntityWithLockingAsync(Guid documentTypeId);
    Task<DocumentNumberDEM> GetDocumentNumberEntityWithLockingAsync(Guid documentTypeId, DbContext context);
}
