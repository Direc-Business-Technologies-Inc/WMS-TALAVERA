using Application.DataTransferObjects.Administration.User;
using Application.DataTransferObjects.System.Modules;
using Application.UseCases.Repositories.Domain.System;
using MediatR;
using Shared.Entities;

namespace Application.UseCases.Queries.System;

public record GetAllModulesQry(DataGridIntent Intent) : IRequest<(IEnumerable<ModuleDataGridDTO> Data, int Count)>;

public class GetAllModulesQryHandler(
    IModuleReadRepo moduleReadRepo)
    : IRequestHandler<GetAllModulesQry, (IEnumerable<ModuleDataGridDTO> Data, int Count)>
{
    public async Task<(IEnumerable<ModuleDataGridDTO> Data, int Count)> Handle(GetAllModulesQry request, CancellationToken cancellationToken)
    {
        return await moduleReadRepo.GetModuleTableDetails(request.Intent);
    }
}
