using Application.DataTransferObjects.Others;
using Shared.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Repositories.Integration.Others;

public interface ICustomerIntegration
{
    Task<(IEnumerable<CustomerDTO> Data, int Count)> GetCustomersListAsync(DataGridIntent intent);
}
