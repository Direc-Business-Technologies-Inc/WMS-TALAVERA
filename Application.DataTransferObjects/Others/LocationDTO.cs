using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Others;

public class LocationDTO
{
    public int Id { get; set; }
    public string LocationNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Subsidiary { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    public int SubsidiaryId { get; set; }
}
