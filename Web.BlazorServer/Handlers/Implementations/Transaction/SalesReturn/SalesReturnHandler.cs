using Application.DataTransferObjects.Transactions.SalesReturn;
using Application.UseCases.Commands.Transaction.SalesReturn;
using Application.UseCases.Queries.Transaction.SalesReturn;
using Domain.Entities.Enums.Transaction.SalesReturn;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Transaction.SalesReturn;
using Web.BlazorServer.ViewModels.Transaction.SalesReturn;

namespace Web.BlazorServer.Handlers.Implementations.Transaction.SalesReturn;

public class SalesReturnHandler(ISender Sender) : ISalesReturnHandler
{
    public async Task<(IEnumerable<SalesReturnDataGridVM> Data, int Count)> GetSalesReturnDataGridAsync(DataGridIntent intent)
    {
        GetSalesReturnDataGridQry qry = new(intent);
        (IEnumerable<SalesReturnDataGridDTO> Data, int Count) = await Sender.Send(qry);
        return (Data.Adapt<IEnumerable<SalesReturnDataGridVM>>(), Count);
    }

    public async Task<SalesReturnVM?> GetSalesReturnAsync(int docEntry)
    {
        GetSalesReturnQry qry = new(docEntry);
        SalesReturnDTO? response = await Sender.Send(qry);
        return response.Adapt<SalesReturnVM?>();
    }

    public async Task<(IEnumerable<SalesReturnRequestDataGridVM> Data, int Count)> GetSalesReturnRequestDataGridAsync(DataGridIntent intent)
    {
        GetSalesReturnRequestDataGridQry qry = new(intent);
        (IEnumerable<SalesReturnRequestDataGridDTO> Data, int Count) = await Sender.Send(qry);
        return (Data.Adapt<IEnumerable<SalesReturnRequestDataGridVM>>(), Count);
    }

    public async Task<SalesReturnRequestVM?> GetSalesReturnRequestAsync(int docEntry)
    {
        GetSalesReturnRequestQry qry = new(docEntry);
        SalesReturnRequestDTO? response = await Sender.Send(qry);
        return response.Adapt<SalesReturnRequestVM?>();
    }

    public async Task<IEnumerable<ReturnTypeVM>> GetReturnTypesAsync()
    {
        GetReturnTypesQry qry = new();
        IEnumerable<ReturnTypeDTO> response = await Sender.Send(qry);
        return response.Adapt<IEnumerable<ReturnTypeVM>>();
    }

    public async Task<bool> PostSalesReturnAsync(SalesReturnVM data, SalesReturnPostingSource source)
    {
        PostSalesReturnCmd cmd = new(data.Adapt<SalesReturnDTO>(), source);
        return await Sender.Send(cmd);
    }

    public async Task<bool> PostSalesReturnFromRequestAsync(SalesReturnRequestVM data)
    {
        SalesReturnRequestDTO requestDto = data.Adapt<SalesReturnRequestDTO>();
        SalesReturnDTO dto = requestDto.Adapt<SalesReturnDTO>();
        dto.DocumentLines = [.. requestDto.DocumentLines.Adapt<IEnumerable<SalesReturnLineDTO>>()];
        PostSalesReturnCmd cmd = new(dto, SalesReturnPostingSource.Request);
        return await Sender.Send(cmd);
    }
}
