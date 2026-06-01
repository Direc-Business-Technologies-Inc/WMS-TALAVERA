using Application.UseCases.Queries.System;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.System;
using Web.BlazorServer.ViewModels.System;

namespace Web.BlazorServer.Handlers.Implementations.System;

public class ModuleHandler(ISender Sender) : IModuleHandler
{
    public async Task<(IEnumerable<ModuleDataGridVM> Data, int Count)> GetModuleTableDetailsAsync(DataGridIntent intent)
    {
        GetAllModulesQry qry = new(intent);
        var response = await Sender.Send(qry);
        List<ModuleDataGridVM> modules = [];
        foreach (var item in response.Data)
        {
            modules.Add(new ModuleDataGridVM
            {
                Id = item.Id,
                Name = item.Name,
                Code = item.Code,
                Active = item.Active,
                Root = item.Root,
                Transactional = item.Transactional,
                Permissions = item.Permissions.Adapt<List<ModulePermissionVM>>()
            });
        }
        return (modules, response.Count);
    }
}
