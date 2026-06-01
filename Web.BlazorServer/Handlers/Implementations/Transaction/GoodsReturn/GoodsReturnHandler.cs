using Application.DataTransferObjects.Transactions.GoodsReturn;
using Application.UseCases.Commands.Transaction.GoodsReturn;
using Application.UseCases.Queries.Transaction.GoodsReturn;
using Domain.Entities.Enums.Transaction.GoodsReturn;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Transaction.GoodsReturn;
using Web.BlazorServer.ViewModels.Transaction.GoodsReturn;

namespace Web.BlazorServer.Handlers.Implementations.Transaction.GoodsReturn
{
    public class GoodsReturnHandler(
        ISender Sender)
        : IGoodsReturnHandler
    {
        public async Task<GoodsReturnVM?> GetGoodsReturnAsync(int docEntry)
        {
            GetGoodsReturnQry qry = new(docEntry);
            GoodsReturnDTO? response = await Sender.Send(qry);

            return response.Adapt<GoodsReturnVM>();
        }

        public async Task<(IEnumerable<GoodsReturnDataGridVM> Data, int Count)> GetGoodsReturnDataGridAsync(DataGridIntent intent)
        {
            GetGoodsReturnsQry qry = new(intent);
            (IEnumerable<GoodsReturnDataGridDTO> Data, int Count) = await Sender.Send(qry);

            return (Data.Adapt<IEnumerable<GoodsReturnDataGridVM>>(), Count);
        }

        public async Task<GoodsReturnRequestVM?> GetGoodsReturnRequestAsync(int docEntry)
        {
            GetGoodsReturnRequestQry qry = new(docEntry);
            GoodsReturnRequestDTO? response = await Sender.Send(qry);

            return response.Adapt<GoodsReturnRequestVM>();
        }

        public async Task<(IEnumerable<GRRDataGridVM> Data, int Count)> GetGoodsReturnRequestDataGridAsync(DataGridIntent intent)
        {
            GetGoodsReturnRequestsQry qry = new(intent);
            (IEnumerable<GRRDataGridDTO> Data, int Count) = await Sender.Send(qry);

            return (Data.Adapt<IEnumerable<GRRDataGridVM>>(), Count);
        }

        public async Task<IEnumerable<ReturnTypeVM>> GetReturnTypesAsync()
        {
            GetReturnTypesQry qry = new();
            IEnumerable<ReturnTypeDTO> response = await Sender.Send(qry);

            return response.Adapt<IEnumerable<ReturnTypeVM>>();
        }

        public async Task<bool> PostGoodsReturnAsync(GoodsReturnRequestVM data, GoodsReturnPostingSource source)
        {
            PostGoodsReturnCmd cmd = new(data.Adapt<GoodsReturnRequestDTO>(), source);
            bool result = await Sender.Send(cmd);

            return result;
        }

        public async Task<bool> PostGoodsReturnAsync(GoodsReturnVM data, GoodsReturnPostingSource source)
        {
            PostGoodsReturnCmd cmd = new(data.Adapt<GoodsReturnRequestDTO>(), source);
            bool result = await Sender.Send(cmd);

            return result;
        }
    }
}
