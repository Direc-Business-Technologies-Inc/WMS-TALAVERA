using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.NS.DataTransferObjects.TripTicket;

public class TripTicketNSDTO
{

    public int NetsuiteTripTicketInternalId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string Driver { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int HelperId { get; set; }
    public string HelperFirstName { get; set; } = string.Empty;
    public string HelperLastName { get; set; } = string.Empty;
    public string TruckPlateName { get; set; } = string.Empty;
    public int TruckPlateId { get; set; }
    public DateTime? TripDate { get; set; }
}
