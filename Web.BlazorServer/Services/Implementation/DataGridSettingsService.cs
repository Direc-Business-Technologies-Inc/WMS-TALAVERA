using Microsoft.JSInterop;
using Radzen;
using System.Text.Json;
using Web.BlazorServer.Services.Repositories;

namespace Web.BlazorServer.Services.Implementation;

public class DataGridSettingsService(
    IJSRuntime jsRuntime) 
    : IDataGridSettingsService
{
    DataGridSettings _settings = new();

    public DataGridSettings Settings 
    { 
        get => _settings; 
        set 
        { 
            _settings = value; 
            _ = SetStateAsync(); 
        } 
    }

    public async Task LoadStateAsync(string dataGridID = "DataGridSettings")
    {
        var result = await jsRuntime.InvokeAsync<string>("localStorage.getItem", dataGridID);

        if(!string.IsNullOrEmpty(result) && result != null)
        {
            _settings = JsonSerializer.Deserialize<DataGridSettings>(result) ?? new DataGridSettings();
        }
    }

    public async Task SetStateAsync(string dataGridID = "DataGridSettings")
    {
        await jsRuntime.InvokeVoidAsync(
            "window.localStorage.setItem",
            "SettingsLoadData",
            JsonSerializer.Serialize(Settings));
    }
}
