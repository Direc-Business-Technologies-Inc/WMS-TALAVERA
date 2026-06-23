using Shared.Entities;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Handlers.Repositories.Others;

public interface ISubsidiaryHandler
{
    Task<(IEnumerable<SubsidiaryVM> Data, int Count)> GetSubsidiariesAsync(DataGridIntent intent);
}
