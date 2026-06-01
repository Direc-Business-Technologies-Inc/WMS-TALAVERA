using Application.DataTransferObjects.System;
using Application.DataTransferObjects.System.Commons;
using Application.UseCases.Repositories.Domain.System;
using Database.MsSql.Core;
using Domain.Entities.System;
using Domain.Entities.Transaction.Common;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Database.MsSql.Implementation.Reads;

public class DocNumReadRepo(IDbContextFactory<AppDbContext> dbContextFactory) : AppDbWork<DocNumReadRepo>, IDocNumReadRepo
{
    public Task<DocumentNumberDEM> GetDocumentNumberEntityWithLockingAsync(Guid documentTypeId, DbContext context)
    {
        return ExecuteAppDbWork<DocumentNumberDEM>(async () =>
        {

            DocumentTypeDEM? documentType = await context.Set<DocumentTypeDEM>().FirstOrDefaultAsync(x => x.Id == documentTypeId) ?? throw new KeyNotFoundException($"Document type not found for Id: {documentTypeId}");

            DocumentNumberDEM? series = (await context.Set<DocumentNumberDEM>().FromSqlInterpolated($@"EXEC APP_SP_GetNextDocumentNumber @DocumentTypeId = {documentTypeId}").ToListAsync()).FirstOrDefault() ?? throw new KeyNotFoundException($"Document number series not found for DocumentTypeId: {documentTypeId}");

            return series;
        });
    }

    public Task<DocumentNumberDEM> GetDocumentNumberEntityWithLockingAsync(Guid documentTypeId)
    {
        throw new NotImplementedException();
    }

    public Task<DocumentNumberDTO> GetDocumentNumberWithLockingAsync(Guid documentTypeId)
    {
        return ExecuteAppDbWork<DocumentNumberDTO>(async () =>
        {
            await using var ctx = await dbContextFactory.CreateDbContextAsync();

            DocumentTypeDEM? documentType = await ctx.ODCT.FirstOrDefaultAsync(x => x.Id == documentTypeId) ?? throw new KeyNotFoundException($"Document type not found for Id: {documentTypeId}");

            DocumentNumberDEM? series = (await ctx.ODCN.FromSqlInterpolated($@"EXEC SP_GetNextDocumentNumber @DocumentTypeId = {documentTypeId}").ToListAsync()).FirstOrDefault() ?? throw new KeyNotFoundException($"Document number series not found for DocumentTypeId: {documentTypeId}");

            DocumentNumberDTO dto = new()
            {
                Id = series.Id,
                DocumentType = documentType.Adapt<DocumentTypeDTO>(),
                Code = series.Code,
                Prefix = series.Prefix,
                CurrentNumber = series.CurrentNumber,
                NextNumber = series.NextNumber,
                CurrentDocNum = series.GenerateCurrentDocNum(),
                NextDocNum = series.GenerateNextDocNum()
            };

            return dto;
        });

    }

    public Task<DocumentNumberDTO> GetDocumentNumberWithLockingAsync(Guid documentTypeId, DbContext context)
    {
        return ExecuteAppDbWork<DocumentNumberDTO>(async () =>
        {

            DocumentTypeDEM? documentType = await context.Set<DocumentTypeDEM>().FirstOrDefaultAsync(x => x.Id == documentTypeId) ?? throw new KeyNotFoundException($"Document type not found for Id: {documentTypeId}");

            DocumentNumberDEM? series = (await context.Set<DocumentNumberDEM>().FromSqlInterpolated($@"EXEC SP_GetNextDocumentNumber @DocumentTypeId = {documentTypeId}").ToListAsync()).FirstOrDefault() ?? throw new KeyNotFoundException($"Document number series not found for DocumentTypeId: {documentTypeId}");

            DocumentNumberDTO dto = new()
            {
                Id = series.Id,
                DocumentType = documentType.Adapt<DocumentTypeDTO>(),
                Code = series.Code,
                Prefix = series.Prefix,
                CurrentNumber = series.CurrentNumber,
                NextNumber = series.NextNumber,
                CurrentDocNum = series.GenerateCurrentDocNum(),
                NextDocNum = series.GenerateNextDocNum()
            };

            return dto;
        });
    }
}
