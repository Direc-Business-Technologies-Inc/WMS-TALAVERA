using Application.UseCases.Repositories.Bases;
using Domain.Entities.Entities.Administration.User.Management;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Commands.Administration.Settings;


public record UpdateSettingCMD(string setting, string value) : ITransactionalRequest<string>;

public class UpdateSettingCMDHandler(
    IAppReadRepository appReadRepo,
    IAppCommandRepository appCommandRepo
    ) : IRequestHandler<UpdateSettingCMD, string>
{
    public async Task<string> Handle(UpdateSettingCMD request, CancellationToken cancellationToken)
    {
        SettingsDEM? setting = await appReadRepo.FirstOrDefaultAsync<SettingsDEM>(x => x.Code.Equals(request.setting));
        if (setting is null) throw new InvalidOperationException($"Couldn't find the setting {request.setting}");

        setting.SetValue(request.value, Guid.Empty);
        appCommandRepo.Update(setting);

        return setting.Value;
    }
}