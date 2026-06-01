using Application.DataTransferObjects.Transactions.InventoryCounting;
using Application.UseCases.Repositories.Bases;
using Application.UseCases.Repositories.Domain.System;
using Application.UseCases.Repositories.Domain.Transaction.InventoryCounting;
using Domain.Entities.Entities.Transaction.InventoryCounting;
using Domain.Entities.System;
using Domain.Entities.Transaction.Common;
using Domain.Entities.ValueObjects.Others;
using Domain.Entities.ValueObjects.Transaction;
using Domain.ValueObjects.Transaction;
using MediatR;

namespace Application.UseCases.Commands.Transaction.InventoryCounting;

public record CreateInventoryCountingDocumentCmd(InventoryCountingDocumentDTO Data) : ITransactionalRequest<bool>;

public class CreateInventoryCountingDocumentCmdHandler(
    IAppCommandRepository appCommandRepo,
    IAppReadRepository appReadRepo,
    IDocNumReadRepo docNumReadRepository,
    IInventoryCountingReadRepo inventoryCountingReadRepo)
    : IRequestHandler<CreateInventoryCountingDocumentCmd, bool>
{
    public async Task<bool> Handle(CreateInventoryCountingDocumentCmd request, CancellationToken cancellationToken)
    {
        DocumentTypeDEM docType = await appReadRepo.FirstOrDefaultAsync<DocumentTypeDEM>(x => x.Name.ToLower().Equals("inventory counting")) ?? throw new Exception("Document Type not found.");

        InventoryCountingDocumentDTO data = request.Data;

        bool duplicate = await inventoryCountingReadRepo
            .ExistsDocumentForWarehouseAndCycleInPeriodAsync(data.Warehouse.WhsCode, data.CycleType, data.CountingDate);

        if (duplicate)
            throw new InvalidOperationException(
                $"An inventory counting document already exists for warehouse '{data.Warehouse.WhsName}' with cycle type '{data.CycleType}' in the same period.");

        DocumentNumberDEM docNum = await docNumReadRepository.GetDocumentNumberEntityWithLockingAsync(docType.Id, appCommandRepo.GetDbContext());

        List<InventoryCountingDocumentLineVO> documentLines = [.. data.DocumentLines.Select(line =>
            new InventoryCountingDocumentLineVO(
                line.ItemCode,
                line.ItemName,
                0, // Initial actual quantity
                line.Quantity,
                line.UoMCode,
                line.UoMValue,
                line.UoMName
            ).SetISBN(line.ISBN))];

        SapDocumentReferenceVO? sapRef = data.SapReference.DocNum == 0 
            ? null 
            : new SapDocumentReferenceVO(data.SapReference.DocEntry, data.SapReference.DocNum);

        var dem = new InventoryCountingDocumentDEM(
            data.DocumentType.Id,
            new AppDocNumVO(docNum.GenerateNextDocNum()),
            new WarehouseVO(data.Warehouse.WhsCode, data.Warehouse.WhsName),
            data.CountingDate,
            data.CycleType,
            documentLines,
            data.Remarks,
            sapRef
        );

        await appCommandRepo.AddAsync(dem);
        return true;
    }
}
