using Application.DataTransferObjects.Others.SAP;
using Application.UseCases.Repositories.Integration.Others;
using Shared.Entities;

namespace Integration.NS.Implementations.Vestigial
{
    public class SchoolYearIntegration : ISchoolYearIntegration
    {
        public Task<(IEnumerable<SchoolYearSAPDTO> Data, int Count)> GetAllSchoolYear(DataGridIntent intent)
        {
            throw new NotImplementedException();
        }
    }
}
