using Application.DataTransferObjects.Administration.Settings;
using Application.UseCases.Repositories.Bases;
using Domain.Entities.Entities.Administration.User.Management;
using Mapster;
using MediatR;

namespace Application.UseCases.Queries.Administration.Settings;

public record GetSettingQry(string code) : IRequest<SettingsDTO?>;

public class GetSettingValueQryHandler(IAppReadRepository appReadRepository)
    : IRequestHandler<GetSettingQry, SettingsDTO?>
{
    public async Task<SettingsDTO?> Handle(GetSettingQry request, CancellationToken cancellationToken)
    {
        var setting = await appReadRepository.FirstOrDefaultAsync<SettingsDEM>(x => x.Code.Equals(request.code));
        if (setting is null) return null;

        return setting.Adapt<SettingsDTO>();
    }
}