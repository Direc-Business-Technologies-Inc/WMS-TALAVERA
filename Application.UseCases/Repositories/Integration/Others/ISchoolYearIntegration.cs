using Application.DataTransferObjects.Others.SAP;
using Shared.Entities;

namespace Application.UseCases.Repositories.Integration.Others;

public interface ISchoolYearIntegration
{
    Task<(IEnumerable<SchoolYearSAPDTO> Data, int Count)> GetAllSchoolYear(DataGridIntent intent);
}
