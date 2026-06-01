using Application.DataTransferObjects.System.Modules;
using Shared.Entities;

namespace Application.UseCases.Repositories.Domain.System;

public interface IModuleReadRepo
{
    Task<(IEnumerable<ModuleDataGridDTO> Data, int Count)> GetModuleTableDetails(DataGridIntent intent);
}
