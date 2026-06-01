namespace Web.BlazorServer.Services.Repositories;

public interface IBusyService
{
    Dictionary<string, bool> States { get; }
    void SetBusy(string key, bool isBusy);
    bool IsBusy(string key);
    bool IsAnyBusy();
    event Action<string, bool>? BusyChanged;
}
