using Application.UseCases.Queries.Administration.Settings;
using Mapster;
using MediatR;
using Web.BlazorServer.Handlers.Repositories.Administration.Settings;
using Web.BlazorServer.ViewModels.Administration.Settings;

namespace Web.BlazorServer.Handlers.Implementations.Administration.Settings
{
    public class SettingsHandler(ISender sender) : ISettingsHandler
    {
        public async Task<SettingsVM?> GetSettingAsync(string code)
        {
            GetSettingQry query = new(code);
            var setting = await sender.Send(query);

            if (setting is null) return null;
            return setting.Adapt<SettingsVM>();
        }

        public async Task<IEnumerable<SettingsVM>> GetSettingsAsync()
        {
            GetAllSettingsQry qry = new();
            var settings = await sender.Send(qry);

            return settings.Adapt<IEnumerable<SettingsVM>>();
        }

        public async Task<string?> GetSettingValueAsync(string code)
        {
            var setting = await GetSettingAsync(code);

            return setting?.Value ?? null;
        }
    }
}
