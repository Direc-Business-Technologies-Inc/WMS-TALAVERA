using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DataTransferObjects.Others;

public class LocationBinDTO
{
    public int Id { get; set; }
    public string BinNumber { get; set; } = string.Empty;
    public string Memo { get; set; } = string.Empty;
}
