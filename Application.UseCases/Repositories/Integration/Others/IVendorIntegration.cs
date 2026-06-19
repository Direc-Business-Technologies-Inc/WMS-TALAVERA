using Application.DataTransferObjects.Others;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Repositories.Integration.Others;

public interface IVendorIntegration
{
    Task<(IEnumerable<VendorDTO> Data, int Count)> GetVendorsListAsync(DataGridIntent intent);
    Task<(IEnumerable<VendorDTO> Data, int Count)> GetVendorsBySubsidiaryListAsync(DataGridIntent intent, int subsidiary);
}
