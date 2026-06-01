using Shared.Entities;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Handlers.Repositories.Others;

public interface ISchoolYearHandler
{
    Task<(IEnumerable<SchoolYearVM> Data, int Count)> GetSchoolYearsAsync(DataGridIntent intent);
}
