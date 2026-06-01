using Web.BlazorServer.Services.Repositories;

namespace Web.BlazorServer.Services.Implementation;

public class BusyService : IBusyService
{
    public Dictionary<string, bool> States { get; set; } = [];

    public event Action<string, bool>? BusyChanged;

    public bool IsAnyBusy()
    {
        lock (States)
        {
            return States.Values.Any(v => v);
        }
    }

    public bool IsBusy(string key)
    {
        lock (States)
        {
            return States.ContainsKey(key) && States[key];
        }
    }

    public void SetBusy(string key, bool isBusy)
    {
        lock(States)
        {
            States[key] = isBusy;
        }
        BusyChanged?.Invoke(key, isBusy);
    }
}
