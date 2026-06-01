using Application.DataTransferObjects.System;
using Shared.Entities;
using Web.BlazorServer.ViewModels.System;

namespace Web.BlazorServer.Handlers.Repositories.System;

public interface IModuleHandler
{
    Task<(IEnumerable<ModuleDataGridVM> Data, int Count)> GetModuleTableDetailsAsync(DataGridIntent intent);
}
