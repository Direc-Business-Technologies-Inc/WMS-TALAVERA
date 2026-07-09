using Application.DataTransferObjects.Administration.Settings;
using Application.UseCases.Repositories.Bases;
using Domain.Entities.Entities.Administration.User.Management;
using Mapster;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Queries.Administration.Settings;

public record GetAllSettingsQry() : IRequest<IEnumerable<SettingsDTO>>;

public class GetAllSettingsQryHandler(
    IAppReadRepository appReadRepository
    ) : IRequestHandler<GetAllSettingsQry, IEnumerable<SettingsDTO>>
{
    public async Task<IEnumerable<SettingsDTO>> Handle(GetAllSettingsQry request, CancellationToken cancellationToken)
    {
        var settings = await appReadRepository.GetAllAsync<SettingsDEM>();
        return settings.Adapt<IEnumerable<SettingsDTO>>();
    }
}
