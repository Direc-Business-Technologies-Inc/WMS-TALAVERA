using Application.DataTransferObjects.Transactions.Commons;
using Application.UseCases.Queries.Others;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Others;
using Web.BlazorServer.ViewModels.Transaction.Commons;

namespace Web.BlazorServer.Handlers.Implementations.Others;

public class ItemMasterDataHandler(
    ISender Sender)
    : IItemMasterDataHandler
{
    public async Task<(IEnumerable<ItemVM> Data, int Count)> GetItemsInWarehouseAsync(DataGridIntent intent, string whsCode, List<string> itemCodes)
    {
        GetItemsInWarehouseQry qry = new(intent, whsCode, itemCodes);
        (IEnumerable<ItemDTO> Data, int Count) response = await Sender.Send(qry);

        return (response.Data.Adapt<IEnumerable<ItemVM>>(), response.Count);
    }

    public async Task<(IEnumerable<ItemVM> Data, int Count)> GetMerchandiseItemsAsync(DataGridIntent intent)
    {
        GetMerchandiseItemsQry qry = new(intent);
        (IEnumerable<ItemDTO> Data, int Count) = await Sender.Send(qry);

        return (Data.Adapt<IEnumerable<ItemVM>>(), Count);
    }

    public async Task<(IEnumerable<ItemVM> Data, int Count)> GetWarehouseItemsAsync(DataGridIntent intent, string whsCode)
    {
        GetAllWarehouseItemsQry qry = new(intent, whsCode);
        (IEnumerable<ItemDTO> Data, int Count) response = await Sender.Send(qry);

        return (response.Data.Adapt<IEnumerable<ItemVM>>(), response.Count);
    }
}
