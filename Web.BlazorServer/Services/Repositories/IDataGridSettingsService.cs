using Radzen;

namespace Web.BlazorServer.Services.Repositories;

public interface IDataGridSettingsService
{
    public DataGridSettings Settings { get; set; }
    public Task LoadStateAsync(string dataGridID = "DataGridSettings");
    public Task SetStateAsync(string dataGridID = "DataGridSettings");

}
