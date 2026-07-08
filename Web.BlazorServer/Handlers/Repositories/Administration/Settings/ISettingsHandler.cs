using Web.BlazorServer.ViewModels.Administration.Settings;

namespace Web.BlazorServer.Handlers.Repositories.Administration.Settings
{
    public interface ISettingsHandler
    {
        Task<SettingsVM?> GetSettingAsync(string code);
        Task<(string code, bool success)> SetSettingAsync(string code, string value);
        Task<string?> GetSettingValueAsync(string code);
        Task<IEnumerable<SettingsVM>> GetSettingsAsync();
    }
}
